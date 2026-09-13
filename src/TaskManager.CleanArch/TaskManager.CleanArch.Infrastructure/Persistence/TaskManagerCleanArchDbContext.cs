using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Infrastructure.Identity;

namespace TaskManager.CleanArch.Infrastructure.Persistence;

public sealed class TaskManagerCleanArchDbContext(DbContextOptions<TaskManagerCleanArchDbContext> options)
    : IdentityDbContext<UsuarioIdentity, IdentityRole, string>(options)
{
    public DbSet<Projeto> Projetos => Set<Projeto>();
    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();
    public DbSet<StatusTarefa> Statuses => Set<StatusTarefa>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagerCleanArchDbContext).Assembly);
    }
}
