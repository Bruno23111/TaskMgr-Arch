using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Infrastructure.Persistence.Mappings;

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
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Codigo = "PENDENTE", Nome = "Pendente", Categoria = Domain.Enums.CategoriaStatus.Pendente, Protegido = true, Versao = 1 },
            new { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Codigo = "EM_ANDAMENTO", Nome = "Em andamento", Categoria = Domain.Enums.CategoriaStatus.EmAndamento, Protegido = true, Versao = 1 },
            new { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Codigo = "CONCLUIDA", Nome = "Concluída", Categoria = Domain.Enums.CategoriaStatus.Concluida, Protegido = true, Versao = 1 });
    }
}
