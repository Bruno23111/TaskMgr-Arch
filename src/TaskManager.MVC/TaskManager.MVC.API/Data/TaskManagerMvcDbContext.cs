using Microsoft.EntityFrameworkCore;

namespace TaskManager.MVC.API.Data;

public sealed class TaskManagerMvcDbContext(DbContextOptions<TaskManagerMvcDbContext> options) : DbContext(options);
