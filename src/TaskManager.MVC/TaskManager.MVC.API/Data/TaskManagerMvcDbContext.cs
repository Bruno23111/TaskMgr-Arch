using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data;

public sealed class TaskManagerMvcDbContext(DbContextOptions<TaskManagerMvcDbContext> options)
    : IdentityDbContext<UsuarioIdentity, IdentityRole, string>(options)
{
    public DbSet<Projeto> Projetos => Set<Projeto>();
    public DbSet<Tarefa> Tarefas => Set<Tarefa>();
    public DbSet<Responsavel> Responsaveis => Set<Responsavel>();
    public DbSet<StatusTarefa> Statuses => Set<StatusTarefa>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskManagerMvcDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        PrepararVersoes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        PrepararVersoes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void PrepararVersoes()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries<IVersionado>())
        {
            var versao = entry.Property(entity => entity.Versao);
            if (entry.State == EntityState.Added)
                versao.CurrentValue = 1;
            else if (entry.State == EntityState.Modified)
                versao.CurrentValue = checked(versao.OriginalValue + 1);
        }
    }
}
