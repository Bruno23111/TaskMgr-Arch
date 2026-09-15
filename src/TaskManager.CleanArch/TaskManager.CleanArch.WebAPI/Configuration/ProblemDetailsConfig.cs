using Microsoft.AspNetCore.Mvc;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.WebAPI.Configuration;

public static class ProblemDetailsConfig
{
    public static IServiceCollection AddTaskManagerProblemDetails(this IServiceCollection services)
    {
        services.AddExceptionHandler<TaskManagerExceptionHandler>();
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.ProblemDetails.Extensions.ContainsKey("code"))
                {
                    return;
                }

                var code = GetDefaultCode(context.ProblemDetails.Status ?? context.HttpContext.Response.StatusCode);
                if (code is not null)
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
}
