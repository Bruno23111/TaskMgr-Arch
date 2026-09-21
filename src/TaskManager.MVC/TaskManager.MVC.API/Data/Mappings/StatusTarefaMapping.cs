using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data.Mappings;

public sealed class StatusTarefaMapping : IEntityTypeConfiguration<StatusTarefa>
{
    public void Configure(EntityTypeBuilder<StatusTarefa> builder)
    {
        builder.ToTable("StatusTarefa");
        builder.HasKey(status => status.Id);
        builder.Property(status => status.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(status => status.Nome).HasMaxLength(60).IsRequired();
        builder.Property(status => status.Categoria).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(status => status.Versao).IsConcurrencyToken();
        builder.HasIndex(status => status.Codigo).IsUnique();
        builder.HasData(
            new StatusTarefa { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Codigo = "PENDENTE", Nome = "Pendente", Categoria = CategoriaStatus.Pendente, Protegido = true },
            new StatusTarefa { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Codigo = "EM_ANDAMENTO", Nome = "Em andamento", Categoria = CategoriaStatus.EmAndamento, Protegido = true },
            new StatusTarefa { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Codigo = "CONCLUIDA", Nome = "Concluída", Categoria = CategoriaStatus.Concluida, Protegido = true });
    }
}
