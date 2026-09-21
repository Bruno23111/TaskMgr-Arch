using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace TaskManager.MVC.API.Configuration;

public sealed class ConcurrencyExceptionHandler : IExceptionHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
        {
            return false;
        }

        var details = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflito de concorrência.",
            Detail = "O recurso foi alterado por outra operação. Recarregue os dados e tente novamente.",
            Type = "https://httpstatuses.com/409",
            Instance = httpContext.Request.Path
        };
        details.Extensions["code"] = "CONCORRENCIA";

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        httpContext.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, details, JsonOptions, cancellationToken);

        return true;
    }
}
