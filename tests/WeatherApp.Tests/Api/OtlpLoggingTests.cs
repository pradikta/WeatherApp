using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using WeatherApp.Application.Interfaces;

namespace WeatherApp.Tests.Api;

[Collection(NonParallelCollection.Name)]
public class OtlpLoggingTests
{
    /// <summary>
    /// Loopback stand-in for an OTLP/HTTP collector, so the tests never leave the machine.
    /// </summary>
    private sealed class FakeCollector : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly Task _loop;
        private readonly List<(string Path, byte[] Body)> _requests = [];

        public FakeCollector()
        {
            var probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            Endpoint = $"http://127.0.0.1:{port}";
            _listener.Prefixes.Add(Endpoint + "/");
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

                    using var buffer = new MemoryStream();
                    await context.Request.InputStream.CopyToAsync(buffer);
                    lock (_requests)
                    {
                        _requests.Add((context.Request.Url!.AbsolutePath, buffer.ToArray()));
                    }

                    context.Response.StatusCode = 200;
                    context.Response.Close();
                }
            });
        }

        public string Endpoint { get; }

        public string Paths { get { lock (_requests) { return string.Join(",", _requests.Select(r => r.Path + ":" + r.Body.Length)); } } }

        public bool Received(string path, string text)
        {
            var needle = System.Text.Encoding.UTF8.GetBytes(text);
            lock (_requests)
            {
                return _requests.Any(r => r.Path == path && r.Body.AsSpan().IndexOf(needle) >= 0);
            }
        }

        public void Dispose()
        {
            _listener.Close();
            _loop.Wait(TimeSpan.FromSeconds(2));
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(params (string Key, string Value)[] settings) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var (key, value) in settings)
            {
                builder.UseSetting(key, value);
            }

            builder.ConfigureServices(services => services.AddSingleton(new Mock<IWeatherService>().Object));
        });

    [Fact]
    public async Task WithEndpoint_ExportsLogsOverOtlp()
    {
        using var collector = new FakeCollector();
        var marker = $"otlp-log-{Guid.NewGuid():N}";

        // Program.cs reads the endpoint eagerly, so it must be a real environment variable, not a factory setting.
        using var environment = new EnvironmentVariables(
            ("OTEL_EXPORTER_OTLP_ENDPOINT", collector.Endpoint),
            ("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf"));

        var factory = CreateFactory();
        factory.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("OtlpTest")
            .LogWarning("{Marker}", marker);

        await factory.DisposeAsync();

        for (var i = 0; i < 20 && !collector.Received("/v1/logs", marker); i++)
        {
            await Task.Delay(250);
        }

        collector.Received("/v1/logs", marker).Should().BeTrue($"the log record should reach the OTLP collector (got: {collector.Paths})");
    }

    [Fact]
    public async Task WithoutEndpoint_StillServesRequests()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/countries");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

internal sealed class EnvironmentVariables : IDisposable
{
    private readonly (string Key, string? Original)[] _originals;

    public EnvironmentVariables(params (string Key, string Value)[] values)
    {
        _originals = values.Select(v => (v.Key, Environment.GetEnvironmentVariable(v.Key))).ToArray();
        foreach (var (key, value) in values)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public void Dispose()
    {
        foreach (var (key, original) in _originals)
        {
            Environment.SetEnvironmentVariable(key, original);
        }
    }
}

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class NonParallelCollection
{
    public const string Name = "NonParallel";
}
