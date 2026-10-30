using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;
using WeatherApp.Application.Exceptions;

namespace WeatherApp.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Extensions =
                {
                    ["errors"] = validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                }
            },
            CityNotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "City not found",
                Detail = notFound.Message
            },
            HttpRequestException => new ProblemDetails
            {
                Status = StatusCodes.Status502BadGateway,
                Title = "Weather provider unavailable",
                Detail = "The external weather service could not be reached or rejected the request."
            },
            BrokenCircuitException => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Weather provider temporarily unavailable",
                Detail = "Requests to the external weather service are paused after repeated failures. Try again shortly."
            },
            TimeoutRejectedException => new ProblemDetails
            {
                Status = StatusCodes.Status504GatewayTimeout,
                Title = "Weather provider timed out",
                Detail = "The external weather service did not respond in time."
            },
            _ => null
        };

        if (problem is null)
        {
            return false;
        }

        logger.LogWarning(exception, "Request failed with {StatusCode}", problem.Status);

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
