using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.WebAPI.Configuration;

public sealed class TaskManagerExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, detail) = exception switch
        {
            TaskManager.CleanArch.Application.Common.ApplicationException applicationException => (ObterStatus(applicationException.Code), applicationException.Code, applicationException.Message),
            DomainRuleViolationException domainException => (StatusCodes.Status409Conflict, domainException.Code, domainException.Message),
            ArgumentException argumentException => (StatusCodes.Status400BadRequest, "DADOS_INVALIDOS", argumentException.Message),
            _ => (StatusCodes.Status500InternalServerError, "ERRO_INTERNO", "Ocorreu um erro interno.")
        };

        httpContext.Response.StatusCode = statusCode;
        return problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = statusCode == StatusCodes.Status409Conflict ? "Conflito." : "Falha na requisição.",
                Detail = detail,
                Type = $"https://httpstatuses.com/{statusCode}",
                Instance = httpContext.Request.Path,
                Extensions = { ["code"] = code }
            }
        });
    }

    private static int ObterStatus(string code) => code switch
    {
        "ACESSO_NEGADO" => StatusCodes.Status403Forbidden,
        "RECURSO_NAO_ENCONTRADO" => StatusCodes.Status404NotFound,
        "CREDENCIAIS_INVALIDAS" => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status409Conflict
    };
}
