using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Services.Status;
using TaskManager.MVC.API.Services.Projects;
using TaskManager.MVC.API.Services.Tasks;
using System.Text.Json.Serialization;

namespace TaskManager.MVC.API.Configuration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagerMvc(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TaskManager");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "A connection string 'ConnectionStrings:TaskManager' deve ser configurada por User Secrets ou variável de ambiente.");
        }

        services.AddControllers().AddJsonOptions(options =>
            options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow);
        services.AddOpenApi();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddTaskManagerProblemDetails();
        services.AddDbContext<TaskManagerMvcDbContext>(options => options.UseNpgsql(connectionString));
        services.AddTaskManagerIdentity(configuration);
        services.AddHealthChecks().AddDbContextCheck<TaskManagerMvcDbContext>(name: "postgresql");
        services.AddScoped<StatusService>();
        services.AddScoped<ProjectService>();
        services.AddScoped<TaskService>();
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();

        return services;
    }
}
