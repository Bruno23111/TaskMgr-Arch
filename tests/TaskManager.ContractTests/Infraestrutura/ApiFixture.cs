using Npgsql;
using TaskManager.ContractTests.Cliente;
using Xunit;

namespace TaskManager.ContractTests.Infraestrutura;

/// <summary>
/// Hospeda uma das APIs em memória sobre um PostgreSQL real.
/// O banco de trabalho é recriado a partir de um banco-modelo antes de cada cenário,
/// garantindo dados iniciais determinísticos: schema migrado, papéis, status do seed e o Gestor inicial.
/// </summary>
public abstract class ApiFixture : IAsyncLifetime
{
    public const string GestorNome = "Gestor Inicial";
    public const string GestorEmail = "gestor@contrato.test";
    public const string GestorSenha = "SenhaSegura123!";

    public const string JwtEmissor = "TaskMgr.ContractTests";
    public const string JwtAudiencia = "TaskMgr.ContractTests.Clientes";
    public const string JwtChaveSecreta = "chave-exclusiva-dos-testes-de-contrato-0123456789";

    private string _connectionStringAdmin = string.Empty;
    private IAsyncDisposable? _host;

    /// <summary>Nome exibido nos testes: "MVC" ou "CleanArch".</summary>
    public abstract string Nome { get; }

    protected abstract string PrefixoBanco { get; }

    public string ConnectionString { get; private set; } = string.Empty;

    public ClienteApi Anonimo { get; private set; } = null!;

    private string BancoTrabalho => $"{PrefixoBanco}_contrato";

    private string BancoModelo => $"{PrefixoBanco}_contrato_modelo";

    protected abstract Task AplicarMigrationsAsync(string connectionString);

    protected abstract (IAsyncDisposable Host, HttpClient Cliente) IniciarApi(IReadOnlyDictionary<string, string?> configuracao);

    public async Task InitializeAsync()
    {
        var postgres = await PostgresCompartilhado.ObterAsync();
        _connectionStringAdmin = postgres.GetConnectionString();
        ConnectionString = new NpgsqlConnectionStringBuilder(_connectionStringAdmin) { Database = BancoTrabalho }.ConnectionString;

        await ExecutarAdminAsync(
            $"DROP DATABASE IF EXISTS {BancoModelo} WITH (FORCE)",
            $"DROP DATABASE IF EXISTS {BancoTrabalho} WITH (FORCE)",
            $"CREATE DATABASE {BancoTrabalho}");

        await AplicarMigrationsAsync(ConnectionString);

        // A inicialização da API cria os papéis e o Gestor inicial no banco de trabalho.
        var (host, cliente) = IniciarApi(Configuracao());
        _host = host;
        Anonimo = new ClienteApi(cliente);

        // O banco de trabalho já pronto vira o modelo dos cenários seguintes.
        LiberarConexoes();
        await ExecutarAdminAsync($"CREATE DATABASE {BancoModelo} TEMPLATE {BancoTrabalho}");
    }

    /// <summary>Descarta tudo o que o cenário anterior gravou.</summary>
    public async Task ReiniciarBancoAsync()
    {
        LiberarConexoes();
        await ExecutarAdminAsync(
            $"DROP DATABASE IF EXISTS {BancoTrabalho} WITH (FORCE)",
            $"CREATE DATABASE {BancoTrabalho} TEMPLATE {BancoModelo}");
        LiberarConexoes();
    }

    /// <summary>
    /// Acesso direto ao banco da API, usado só para preparar falhas (CA17) ou
    /// verificar estado que a API não expõe, como a exclusão lógica.
    /// </summary>
    public async Task<T?> ConsultarEscalarAsync<T>(string sql, params (string Nome, object Valor)[] parametros)
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        foreach (var (nome, valor) in parametros)
        {
            comando.Parameters.AddWithValue(nome, valor);
        }

        var resultado = await comando.ExecuteScalarAsync();
        return resultado is null or DBNull ? default : (T)resultado;
    }

    public async Task ExecutarSqlAsync(string sql)
    {
        await using var conexao = new NpgsqlConnection(ConnectionString);
        await conexao.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conexao);
        await comando.ExecuteNonQueryAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync();
        }
    }

    private Dictionary<string, string?> Configuracao() => new()
    {
        ["ConnectionStrings:TaskManager"] = ConnectionString,
        ["Jwt:Emissor"] = JwtEmissor,
        ["Jwt:Audiencia"] = JwtAudiencia,
        ["Jwt:ChaveSecreta"] = JwtChaveSecreta,
        ["Jwt:DuracaoEmMinutos"] = "30",
        ["GestorInicial:Nome"] = GestorNome,
        ["GestorInicial:Email"] = GestorEmail,
        ["GestorInicial:Senha"] = GestorSenha,
    };

    private void LiberarConexoes()
    {
        using var conexao = new NpgsqlConnection(ConnectionString);
        NpgsqlConnection.ClearPool(conexao);
    }

    private async Task ExecutarAdminAsync(params string[] comandos)
    {
        await using var conexao = new NpgsqlConnection(_connectionStringAdmin);
        await conexao.OpenAsync();
        foreach (var sql in comandos)
        {
            await using var comando = new NpgsqlCommand(sql, conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }
}
