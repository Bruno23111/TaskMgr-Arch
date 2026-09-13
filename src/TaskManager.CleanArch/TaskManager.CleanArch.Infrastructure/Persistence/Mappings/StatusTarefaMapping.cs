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
    }
}
