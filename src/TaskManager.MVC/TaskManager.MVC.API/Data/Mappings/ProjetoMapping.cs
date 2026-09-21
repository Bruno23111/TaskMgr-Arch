using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Data.Mappings;

public sealed class ProjetoMapping : IEntityTypeConfiguration<Projeto>
{
    public void Configure(EntityTypeBuilder<Projeto> builder)
    {
        builder.ToTable("Projetos");
        builder.HasKey(projeto => projeto.Id);
        builder.Property(projeto => projeto.Nome).HasMaxLength(100).IsRequired();
        builder.Property(projeto => projeto.Descricao).HasMaxLength(255);
        builder.Property(projeto => projeto.Versao).IsConcurrencyToken();
        builder.HasQueryFilter(projeto => projeto.ExcluidoEm == null);
    }
}
