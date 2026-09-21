using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data.Mappings;

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
        builder.HasOne<TaskManager.MVC.API.Identity.UsuarioIdentity>().WithOne()
            .HasForeignKey<Responsavel>(responsavel => responsavel.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
