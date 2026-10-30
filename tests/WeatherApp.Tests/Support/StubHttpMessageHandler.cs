namespace WeatherApp.Tests.Support;

/// <summary>
/// Answers every request from memory, so no test can reach the network.
/// </summary>
public sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request.RequestUri!.PathAndQuery);
        return Task.FromResult(responder(request));
    }
}
