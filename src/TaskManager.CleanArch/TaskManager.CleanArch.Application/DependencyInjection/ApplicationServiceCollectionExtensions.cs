using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CleanArch.Application.Abstractions.Services;

namespace TaskManager.CleanArch.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagerApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(
                typeof(ApplicationServiceCollectionExtensions).Assembly));
                
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        
        return services;
    }
}
