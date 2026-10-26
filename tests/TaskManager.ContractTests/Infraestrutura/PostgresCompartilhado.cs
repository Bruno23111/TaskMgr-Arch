using Testcontainers.PostgreSql;

namespace TaskManager.ContractTests.Infraestrutura;

/// <summary>
/// Um único container PostgreSQL para toda a execução. Cada API recebe bancos próprios
/// dentro dele, de modo que as duas suítes nunca compartilham dados mutáveis.
/// </summary>
internal static class PostgresCompartilhado
{
    // Mesma imagem do docker-compose.yml, para que testes e ambiente local usem a mesma versão.
    private const string Imagem = "postgres:15-alpine";

    private static readonly Lazy<Task<PostgreSqlContainer>> Container = new(IniciarAsync);

    public static Task<PostgreSqlContainer> ObterAsync() => Container.Value;

    private static async Task<PostgreSqlContainer> IniciarAsync()
    {
        var container = new PostgreSqlBuilder(Imagem).Build();

        await container.StartAsync();

        // O processo de testes termina com o runner; o Ryuk do Testcontainers remove o container.
        return container;
    }
}
