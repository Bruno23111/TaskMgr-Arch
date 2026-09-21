using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.MVC.API.Data;

public sealed class TaskManagerMvcDbContextFactory : IDesignTimeDbContextFactory<TaskManagerMvcDbContext>
{
    public TaskManagerMvcDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<TaskManagerMvcDbContextFactory>()
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("TaskManager")
            ?? throw new InvalidOperationException("Configure ConnectionStrings:TaskManager nos User Secrets do MVC ou ConnectionStrings__TaskManager no ambiente.");
        var options = new DbContextOptionsBuilder<TaskManagerMvcDbContext>()
            .UseNpgsql(connectionString).Options;
        return new TaskManagerMvcDbContext(options);
    }
}
