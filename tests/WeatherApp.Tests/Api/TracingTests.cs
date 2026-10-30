using System.Diagnostics;
using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WeatherApp.Application;

namespace WeatherApp.Tests.Api;

/// <summary>
/// Verifies that one request produces a single trace in which the geocoding call and the weather call
/// are separate, individually diagnosable spans. The upstream is a loopback listener (no live network)
/// and the real HttpClient pipeline is used, because custom in-memory handlers bypass HTTP instrumentation.
/// </summary>
[Collection(NonParallelCollection.Name)]
public class TracingTests
{
    private sealed class FakeOpenWeatherMap : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Task _loop;

        public FakeOpenWeatherMap(Func<string, int> statusFor)
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Address = $"http://127.0.0.1:{port}/";
            _listener.Prefixes.Add(Address);
            _listener.Start();
            _loop = Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = await _listener.GetContextAsync();
                    }
                    catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
                    {
                        return;
                    }

                    var path = context.Request.Url!.AbsolutePath;
                    var status = statusFor(path);
                    var body = path.StartsWith("/geo/")
                        ? """[{"name":"Jakarta","lat":-6.2,"lon":106.8,"country":"ID"}]"""
                        : "{}";
                    var bytes = System.Text.Encoding.UTF8.GetBytes(body);
                    context.Response.StatusCode = status;
                    context.Response.ContentType = "application/json";
                    await context.Response.OutputStream.WriteAsync(bytes);
                    context.Response.Close();
                }
            });
        }

        public string Address { get; }

        public void Dispose()
        {
            _listener.Close();
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
    }

    [Fact]
    public async Task WeatherCallFailingAfterGeocodeSucceeds_ShowsSeparateSpansInOneTrace()
    {
        using var upstream = new FakeOpenWeatherMap(path => path.StartsWith("/geo/") ? 200 : 500);
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source =>
                source.Name is "System.Net.Http" or "Microsoft.AspNetCore" || source.Name == WeatherTelemetry.Name,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => { lock (spans) { spans.Add(activity); } }
        };
        ActivitySource.AddActivityListener(listener);

        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("OpenWeatherMap:ApiKey", "test-key");
            builder.UseSetting("OpenWeatherMap:BaseAddress", upstream.Address);
            builder.UseSetting("Resilience:Retry:MaxRetryAttempts", "1");
            builder.UseSetting("Resilience:Retry:Delay", "00:00:00.001");
            builder.UseSetting("Resilience:CircuitBreaker:MinimumThroughput", "100");
        });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/weather/Jakarta");

        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);

        List<Activity> snapshot;
        lock (spans)
        {
            snapshot = [.. spans];
        }

        var request = snapshot.Single(a => a.Source.Name == "Microsoft.AspNetCore");
        var traceSpans = snapshot.Where(a => a.TraceId == request.TraceId).ToList();
        var httpSpans = traceSpans.Where(a => a.Source.Name == "System.Net.Http").ToList();

        string UrlOf(Activity a) => a.GetTagItem("url.full")?.ToString() ?? string.Empty;

        var geocode = httpSpans.Where(a => UrlOf(a).Contains("/geo/")).ToList();
        var weather = httpSpans.Where(a => UrlOf(a).Contains("/data/2.5/weather")).ToList();

        geocode.Should().ContainSingle("the geocoding call succeeded once");
        geocode[0].Status.Should().NotBe(ActivityStatusCode.Error);
        geocode[0].GetTagItem("http.response.status_code").Should().Be(200);

        weather.Should().HaveCount(2, "one attempt plus one retry, each as its own span");
        weather.Should().OnlyContain(a => a.Status == ActivityStatusCode.Error);
        weather.Should().OnlyContain(a => (int)a.GetTagItem("http.response.status_code")! == 500);

        traceSpans.Select(a => a.OperationName).Should().Contain("FetchCurrentWeather");
        UrlOf(weather[0]).Should().NotContain("test-key", "the API key must not leak into span attributes");
        UrlOf(geocode[0]).Should().NotContain("test-key", "the API key must not leak into span attributes");
    }
}
