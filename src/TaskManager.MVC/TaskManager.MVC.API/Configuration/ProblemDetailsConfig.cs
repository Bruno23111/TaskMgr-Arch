using Microsoft.AspNetCore.Mvc;

namespace TaskManager.MVC.API.Configuration;

public static class ProblemDetailsConfig
{
    public static IServiceCollection AddTaskManagerProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddExceptionHandler<ConcurrencyExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                var status = context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode;
                context.ProblemDetails.Status = status;
                context.ProblemDetails.Type ??= $"https://httpstatuses.com/{status}";
                context.ProblemDetails.Title ??= GetDefaultTitle(status);
                context.ProblemDetails.Detail ??= context.ProblemDetails.Title;
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

                if (!context.ProblemDetails.Extensions.ContainsKey("code") && GetDefaultCode(status) is { } code)
                {
                    context.ProblemDetails.Extensions["code"] = code;
                }
            };
        });

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var details = new ValidationProblemDetails(context.ModelState)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dados inválidos.",
                    Detail = "Um ou mais campos são inválidos.",
                    Type = "https://httpstatuses.com/400",
                    Instance = context.HttpContext.Request.Path
                };
                details.Extensions["code"] = "DADOS_INVALIDOS";

                return new BadRequestObjectResult(details)
                {
                    ContentTypes = { "application/problem+json" }
                };
            };
        });

        return services;
    }

    private static string? GetDefaultCode(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "DADOS_INVALIDOS",
        StatusCodes.Status401Unauthorized => "NAO_AUTENTICADO",
        StatusCodes.Status403Forbidden => "ACESSO_NEGADO",
        StatusCodes.Status404NotFound => "RECURSO_NAO_ENCONTRADO",
        StatusCodes.Status500InternalServerError => "ERRO_INTERNO",
        _ => null
    };

    private static string GetDefaultTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Dados inválidos.",
        StatusCodes.Status401Unauthorized => "Não autenticado.",
        StatusCodes.Status403Forbidden => "Acesso negado.",
        StatusCodes.Status404NotFound => "Recurso não encontrado.",
        StatusCodes.Status500InternalServerError => "Erro interno.",
        _ => "Erro ao processar a requisição."
    };
}
