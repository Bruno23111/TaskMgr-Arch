using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.WebAPI.Services;

namespace TaskManager.CleanArch.WebAPI.Configuration;

public static class WebApiServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagerWebApi(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioAtual, UsuarioAtualHttpContext>();
        services.AddSingleton<IRelogio, RelogioSistema>();

        return services;
    }
}
