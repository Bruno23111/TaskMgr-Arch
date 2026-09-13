using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Infrastructure.Identity;
using TaskManager.CleanArch.Infrastructure.Persistence;

namespace TaskManager.CleanArch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskManager");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'ConnectionStrings:TaskManager' deve ser configurada por User Secrets ou variável de ambiente.");
        }

        services.AddDbContext<TaskManagerCleanArchDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTaskManagerIdentity();
        services.AddTaskManagerJwt(configuration);
        services.AddScoped<IProjetoRepository, ProjetoRepository>();
        services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();
        services.AddScoped<ITarefaRepository, TarefaRepository>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddHealthChecks().AddDbContextCheck<TaskManagerCleanArchDbContext>(name: "postgresql");

        return services;
    }

    private static IServiceCollection AddTaskManagerIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<UsuarioIdentity>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<TaskManagerCleanArchDbContext>();

        return services;
    }

    private static IServiceCollection AddTaskManagerJwt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.NomeSecao));
        services.AddScoped<ITokenService, JwtTokenService>();

        return services;
    }
}
