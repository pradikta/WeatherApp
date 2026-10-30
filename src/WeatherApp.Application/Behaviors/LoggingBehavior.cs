using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace WeatherApp.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation("Handling {RequestName} with payload {@Payload}", requestName, request);

        try
        {
            var response = await next(cancellationToken);

            stopwatch.Stop();
            logger.LogInformation("Handled {RequestName} in {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Handling {RequestName} failed after {ElapsedMilliseconds} ms", requestName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
