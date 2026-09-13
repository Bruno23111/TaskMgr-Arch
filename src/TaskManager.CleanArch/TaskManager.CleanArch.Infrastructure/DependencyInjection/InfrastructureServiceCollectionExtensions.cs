using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
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
        services.AddScoped<IProjetoRepository, ProjetoRepository>();
        services.AddScoped<IResponsavelRepository, ResponsavelRepository>();
        services.AddScoped<IStatusRepository, StatusRepository>();
        services.AddScoped<ITarefaRepository, TarefaRepository>();
        services.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();
        services.AddHealthChecks().AddDbContextCheck<TaskManagerCleanArchDbContext>(name: "postgresql");

        return services;
    }
}
