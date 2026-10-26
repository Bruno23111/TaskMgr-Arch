extern alias mvc;
extern alias cleanarch;

using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Infrastructure.Persistence;
using Xunit;
using MvcDbContext = mvc::TaskManager.MVC.API.Data.TaskManagerMvcDbContext;

namespace TaskManager.ContractTests.Infraestrutura;

public sealed class MvcApiFixture : ApiFixture
{
    public override string Nome => "MVC";

    protected override string PrefixoBanco => "taskmgr_mvc";

    protected override async Task AplicarMigrationsAsync(string connectionString)
    {
        var opcoes = new DbContextOptionsBuilder<MvcDbContext>().UseNpgsql(connectionString).Options;
        await using var contexto = new MvcDbContext(opcoes);
        await contexto.Database.MigrateAsync();
    }

    protected override (IAsyncDisposable Host, HttpClient Cliente) IniciarApi(IReadOnlyDictionary<string, string?> configuracao)
    {
        var factory = new ApiFactory<mvc::Program>(configuracao);
        return (factory, factory.CreateClient());
    }
}

public sealed class CleanArchApiFixture : ApiFixture
{
    public override string Nome => "CleanArch";

    protected override string PrefixoBanco => "taskmgr_cleanarch";

    protected override async Task AplicarMigrationsAsync(string connectionString)
    {
        var opcoes = new DbContextOptionsBuilder<TaskManagerCleanArchDbContext>().UseNpgsql(connectionString).Options;
        await using var contexto = new TaskManagerCleanArchDbContext(opcoes);
        await contexto.Database.MigrateAsync();
    }

    protected override (IAsyncDisposable Host, HttpClient Cliente) IniciarApi(IReadOnlyDictionary<string, string?> configuracao)
    {
        var factory = new ApiFactory<cleanarch::Program>(configuracao);
        return (factory, factory.CreateClient());
    }
}

// Cada coleção roda seus cenários em série (o banco é reiniciado a cada um);
// as duas coleções rodam em paralelo, pois usam bancos diferentes.
[CollectionDefinition(Nome)]
public sealed class ColecaoMvc : ICollectionFixture<MvcApiFixture>
{
    public const string Nome = "API MVC";
}

[CollectionDefinition(Nome)]
public sealed class ColecaoCleanArch : ICollectionFixture<CleanArchApiFixture>
{
    public const string Nome = "API CleanArch";
}
