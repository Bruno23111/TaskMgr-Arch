using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data.Mappings;

public sealed class TarefaMapping : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> builder)
    {
        builder.ToTable("Tarefas");
        builder.HasKey(tarefa => tarefa.Id);
        builder.Property(tarefa => tarefa.Titulo).HasMaxLength(100).IsRequired();
        builder.Property(tarefa => tarefa.Descricao).HasMaxLength(255);
        builder.Property(tarefa => tarefa.Versao).IsConcurrencyToken();
        builder.HasIndex(tarefa => tarefa.ProjetoId);
        builder.HasIndex(tarefa => tarefa.ResponsavelId);
        builder.HasIndex(tarefa => tarefa.StatusId);
        builder.HasQueryFilter(tarefa => tarefa.ExcluidoEm == null);
        builder.HasOne<Projeto>().WithMany().HasForeignKey(tarefa => tarefa.ProjetoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Responsavel>().WithMany().HasForeignKey(tarefa => tarefa.ResponsavelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StatusTarefa>().WithMany().HasForeignKey(tarefa => tarefa.StatusId).OnDelete(DeleteBehavior.Restrict);
    }
}
