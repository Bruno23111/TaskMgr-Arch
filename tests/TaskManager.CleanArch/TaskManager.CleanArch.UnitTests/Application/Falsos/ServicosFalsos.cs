using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.UnitTests.Application.Falsos;

internal sealed record UsuarioFalso(string UsuarioId, bool Autenticado, bool Gestor) : IUsuarioAtual
{
    public static UsuarioFalso Anonimo { get; } = new(string.Empty, Autenticado: false, Gestor: false);
}

internal sealed class RelogioFixo : IRelogio
{
    public DateTimeOffset DataHoraAgoraUTC { get; set; } = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
}

/// <summary>
/// Executa as operações sem banco e registra o que o handler pediu: quantas gravações
/// e em qual nível de transação. Erros só são relançados, como faria o rollback real.
/// </summary>
internal sealed class UnidadeDeTrabalhoFalsa : IUnidadeDeTrabalho
{
    public int Gravacoes { get; private set; }

    public int TransacoesSerializaveis { get; private set; }

    public int TransacoesSimples { get; private set; }

    public Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        Gravacoes++;
        return Task.FromResult(1);
    }

    public Task<T> ExecutarEmTransacaoAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken = default)
    {
        TransacoesSimples++;
        return operacao(cancellationToken);
    }

    public Task<T> ExecutarEmTransacaoSerializavelAsync<T>(Func<CancellationToken, Task<T>> operacao, CancellationToken cancellationToken = default)
    {
        TransacoesSerializaveis++;
        return operacao(cancellationToken);
    }
}

internal sealed class DespachanteFalso : IDomainEventDispatcher
{
    public List<IDomainEvent> Despachados { get; } = [];

    public Task DespacharAsync(IReadOnlyCollection<IDomainEvent> eventos, CancellationToken cancellationToken = default)
    {
        Despachados.AddRange(eventos);
        return Task.CompletedTask;
    }
}

internal sealed class TokenServiceFalso : ITokenService
{
    public DadosUsuarioToken? Ultimo { get; private set; }

    public TokenAcesso Gerar(DadosUsuarioToken usuario)
    {
        Ultimo = usuario;
        return new TokenAcesso($"token-de-{usuario.UsuarioId}", new DateTimeOffset(2026, 1, 15, 12, 30, 0, TimeSpan.Zero));
    }
}

/// <summary>Contas de autenticação em memória, no lugar do ASP.NET Core Identity.</summary>
internal sealed class ContasEmMemoria : IContaIdentityService
{
    private sealed record Conta(string UsuarioId, string Email, string Senha, bool Ativo, string Papel);

    private readonly Dictionary<string, Conta> _contas = [];

    public string Adicionar(string email, string senha, string papel, bool ativo = true)
    {
        var usuarioId = Guid.NewGuid().ToString();
        _contas[usuarioId] = new Conta(usuarioId, email, senha, ativo, papel);
        return usuarioId;
    }

    public bool Existe(string usuarioId) => _contas.ContainsKey(usuarioId);

    public Task<ContaAutenticada?> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        var conta = _contas.Values.SingleOrDefault(conta =>
            string.Equals(conta.Email, email, StringComparison.OrdinalIgnoreCase) && conta.Senha == senha && conta.Ativo);
        return Task.FromResult(conta is null ? null : new ContaAutenticada(conta.UsuarioId, conta.Email, [conta.Papel]));
    }

    public Task<ContaAutenticada?> ObterAtivaAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_contas.TryGetValue(usuarioId, out var conta) && conta.Ativo
            ? new ContaAutenticada(conta.UsuarioId, conta.Email, [conta.Papel])
            : null);

    public Task<ContaIdentityDto?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_contas.TryGetValue(usuarioId, out var conta) ? ParaDto(conta) : null);

    public Task<IReadOnlyDictionary<string, ContaIdentityDto>> ObterContasAsync(IEnumerable<string> usuarioIds, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, ContaIdentityDto>>(usuarioIds
            .Where(_contas.ContainsKey)
            .ToDictionary(id => id, id => ParaDto(_contas[id])));

    public Task<ContaIdentityDto> CriarContaAsync(CriarContaIdentity dados, CancellationToken cancellationToken = default)
    {
        if (_contas.Values.Any(conta => string.Equals(conta.Email, dados.Email, StringComparison.OrdinalIgnoreCase)))
            throw new ValorDuplicadoException("O e-mail já está em uso.");

        var usuarioId = Adicionar(dados.Email, dados.Senha, dados.Papel);
        return Task.FromResult(ParaDto(_contas[usuarioId]));
    }

    public Task AtualizarContaAsync(string usuarioId, string email, string papel, CancellationToken cancellationToken = default)
    {
        _contas[usuarioId] = _contas[usuarioId] with { Email = email, Papel = papel };
        return Task.CompletedTask;
    }

    public Task DesativarContaAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        _contas[usuarioId] = _contas[usuarioId] with { Ativo = false };
        return Task.CompletedTask;
    }

    public Task ExcluirContaAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        _contas.Remove(usuarioId);
        return Task.CompletedTask;
    }

    public Task<int> ContarGestoresAtivosAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_contas.Values.Count(conta => conta.Ativo && conta.Papel == PapeisSistema.Gestor));

    private static ContaIdentityDto ParaDto(Conta conta) => new(conta.UsuarioId, conta.Email, conta.Ativo, [conta.Papel]);
}
