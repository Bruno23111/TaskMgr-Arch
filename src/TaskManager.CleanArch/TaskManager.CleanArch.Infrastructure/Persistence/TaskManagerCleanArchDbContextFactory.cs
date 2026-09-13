using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class TaskManagerCleanArchDbContextFactory : IDesignTimeDbContextFactory<TaskManagerCleanArchDbContext>
{
    public TaskManagerCleanArchDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TASKMANAGER_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=taskmgr_cleanarch;Username=taskmgr;Password=design-time-only";

        var options = new DbContextOptionsBuilder<TaskManagerCleanArchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TaskManagerCleanArchDbContext(options);
    }
}
