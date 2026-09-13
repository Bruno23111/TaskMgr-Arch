using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Infrastructure.Persistence.Mappings;

public sealed class ResponsavelMapping : IEntityTypeConfiguration<Responsavel>
{
    public void Configure(EntityTypeBuilder<Responsavel> builder)
    {
        builder.ToTable("Responsaveis");
        builder.HasKey(responsavel => responsavel.Id);
        builder.Property(responsavel => responsavel.UsuarioId).HasMaxLength(450).IsRequired();
        builder.Property(responsavel => responsavel.Nome).HasMaxLength(100).IsRequired();
        builder.Property(responsavel => responsavel.Versao).IsConcurrencyToken();
        builder.HasIndex(responsavel => responsavel.UsuarioId).IsUnique();
    }
}
