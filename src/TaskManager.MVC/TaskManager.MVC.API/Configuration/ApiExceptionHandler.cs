using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace TaskManager.MVC.API.Configuration;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            return false;
        }

        var details = new ProblemDetails
        {
            Status = apiException.Status,
            Title = apiException.Title,
            Detail = apiException.Message,
            Type = $"https://httpstatuses.com/{apiException.Status}",
            Instance = httpContext.Request.Path
        };
        details.Extensions["code"] = apiException.Code;

        httpContext.Response.StatusCode = apiException.Status;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, details, JsonOptions, cancellationToken);
        return true;
    }
}
