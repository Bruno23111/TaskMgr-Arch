using Microsoft.EntityFrameworkCore;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class TaskManagerCleanArchDbContext(DbContextOptions<TaskManagerCleanArchDbContext> options) : DbContext(options);
