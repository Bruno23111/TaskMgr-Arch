using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.Infrastructure.Persistence;
using Xunit;

namespace TaskManager.ApiParity.Tests;

// Regressão do DEF-002: a tarefa nova nasce no status protegido PENDENTE do seed (como no MVC),
// mesmo que exista status adicional da categoria Pendente com código anterior em ordem alfabética.
public sealed class StatusInicialDaTarefaTests
{
    private static readonly Guid PendenteDoSeed = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Theory]
    [InlineData("BACKLOG")]
    [InlineData("A_FAZER")]
    [InlineData("ZZZ_DEPOIS")]
    public async Task StatusInicialEhOPendenteProtegidoMesmoComStatusAdicionalDaCategoria(string codigoAdicional)
    {
        await using var context = CriarContexto();
        context.Statuses.Add(StatusTarefa.Criar(codigoAdicional, "Adicional", CategoriaStatus.Pendente));
        await context.SaveChangesAsync();

        var inicial = await new StatusRepository(context).ObterPendenteInicialAsync();

        Assert.NotNull(inicial);
        Assert.Equal(PendenteDoSeed, inicial.Id);
        Assert.True(inicial.Protegido);
    }

    private static TaskManagerCleanArchDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<TaskManagerCleanArchDbContext>()
            .UseInMemoryDatabase($"status-inicial-{Guid.NewGuid()}")
            .Options;

        var context = new TaskManagerCleanArchDbContext(options);
        context.Database.EnsureCreated(); // aplica o seed dos três status protegidos
        return context;
    }
}
