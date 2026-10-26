using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>
/// Base de todos os cenários. Cada cenário começa com o banco recém-criado a partir do modelo,
/// o Gestor inicial autenticado e os três status do seed resolvidos pelo código, nunca por ID fixo.
/// </summary>
public abstract class CenarioDeContrato(ApiFixture api) : IAsyncLifetime
{
    public const string SenhaPadrao = "SenhaUsuario123!";

    protected ApiFixture Api { get; } = api;

    protected ClienteApi Anonimo => Api.Anonimo;

    protected ClienteApi Gestor { get; private set; } = null!;

    protected StatusTarefa Pendente { get; private set; } = null!;

    protected StatusTarefa EmAndamento { get; private set; } = null!;

    protected StatusTarefa Concluida { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Api.ReiniciarBancoAsync();
        Gestor = await AutenticarAsync(ApiFixture.GestorEmail, ApiFixture.GestorSenha);

        var status = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);
        Pendente = status.Single(s => s.Codigo == Categorias.Pendente);
        EmAndamento = status.Single(s => s.Codigo == Categorias.EmAndamento);
        Concluida = status.Single(s => s.Codigo == Categorias.Concluida);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Texto único e curto (cabe no limite de 60 caracteres do nome de status).</summary>
    protected static string Unico(string prefixo) => $"{prefixo} {Guid.NewGuid().ToString("N")[..12]}";

    protected static string EmailUnico() => $"{Guid.NewGuid():N}@contrato.test";

    protected async Task<ClienteApi> AutenticarAsync(string email, string senha)
    {
        var login = await Anonimo.PostAsync(Rotas.Login, new { email, senha })
            .DeveRetornarAsync<LoginResposta>(HttpStatusCode.OK);
        return Anonimo.ComToken(login.AccessToken);
    }

    protected Task<Projeto> CriarProjetoAsync(string? nome = null) =>
        Gestor.PostAsync(Rotas.Projetos, new { nome = nome ?? Unico("Projeto"), descricao = "Criado pelo teste de contrato" })
            .DeveRetornarAsync<Projeto>(HttpStatusCode.Created);

    protected Task<Projeto> ObterProjetoAsync(Guid id) =>
        Gestor.GetAsync(Rotas.Projeto(id)).DeveRetornarAsync<Projeto>(HttpStatusCode.OK);

    protected Task<Tarefa> CriarTarefaAsync(Guid projetoId, string? titulo = null) =>
        Gestor.PostAsync(Rotas.Tarefas, new { projetoId, titulo = titulo ?? Unico("Tarefa"), descricao = "Descrição" })
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.Created);

    protected Task<Tarefa> ObterTarefaAsync(Guid id) =>
        Gestor.GetAsync(Rotas.Tarefa(id)).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

    protected Task<Paginado<Tarefa>> ListarTarefasDoProjetoAsync(Guid projetoId) =>
        Gestor.GetAsync(Rotas.TarefasDoProjeto(projetoId)).DeveRetornarAsync<Paginado<Tarefa>>(HttpStatusCode.OK);

    /// <summary>Cria conta e perfil via API (único caminho, não há cadastro público) e faz login.</summary>
    protected async Task<(Responsavel Perfil, ClienteApi Cliente)> CriarContaAsync(string papel = Papeis.Usuario)
    {
        var email = EmailUnico();
        var perfil = await Gestor.PostAsync(Rotas.Responsaveis, new { nome = Unico(papel), email, senha = SenhaPadrao, papel })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.Created);
        return (perfil, await AutenticarAsync(email, SenhaPadrao));
    }

    protected Task<Responsavel> ObterResponsavelAsync(Guid id) =>
        Gestor.GetAsync(Rotas.Responsavel(id)).DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);

    protected async Task<Responsavel> PerfilDoGestorInicialAsync()
    {
        var todos = await Gestor.GetAsync(Rotas.Responsaveis).DeveRetornarAsync<List<Responsavel>>(HttpStatusCode.OK);
        return todos.Single(r => string.Equals(r.Email, ApiFixture.GestorEmail, StringComparison.OrdinalIgnoreCase));
    }

    protected Task<HttpResponseMessage> MudarStatusAsync(ClienteApi cliente, Tarefa tarefa, StatusTarefa destino, int? versao = null) =>
        cliente.PatchAsync(Rotas.StatusDaTarefa(tarefa.Id), new { statusId = destino.Id, versao = versao ?? tarefa.Versao });

    protected Task<Tarefa> MudarStatusComSucessoAsync(Tarefa tarefa, StatusTarefa destino, ClienteApi? cliente = null) =>
        MudarStatusAsync(cliente ?? Gestor, tarefa, destino).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

    protected Task<Tarefa> AtribuirAsync(Tarefa tarefa, Guid responsavelId) =>
        Gestor.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId, versao = tarefa.Versao })
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

    /// <summary>
    /// Leva uma tarefa nova até a categoria pedida pelo caminho permitido da matriz,
    /// atribuindo o responsável quando informado.
    /// </summary>
    protected async Task<Tarefa> TarefaEmAsync(string categoria, Guid? responsavelId = null, Guid? projetoId = null)
    {
        var tarefa = await CriarTarefaAsync(projetoId ?? (await CriarProjetoAsync()).Id);
        if (responsavelId is { } id)
        {
            tarefa = await AtribuirAsync(tarefa, id);
        }

        if (categoria is Categorias.EmAndamento or Categorias.Concluida)
        {
            tarefa = await MudarStatusComSucessoAsync(tarefa, EmAndamento);
        }

        if (categoria is Categorias.Concluida)
        {
            tarefa = await MudarStatusComSucessoAsync(tarefa, Concluida);
        }

        return tarefa;
    }

    /// <summary>Em toda rejeição, o estado persistido deve continuar idêntico.</summary>
    protected async Task TarefaNaoMudouAsync(Tarefa antes) =>
        Assert.Equal(antes, await ObterTarefaAsync(antes.Id));
}
