using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Contracts.Tasks;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Models;
using TaskManager.MVC.API.Services.Tasks;
using TaskManager.MVC.UnitTests.Infraestrutura;
using Xunit;

namespace TaskManager.MVC.UnitTests;

/// <summary>
/// TaskService sobre o provedor em memória. Criar, atribuir e mudar status abrem transação
/// serializável com SELECT ... FOR UPDATE; desses caminhos só as verificações iniciais
/// (permissão e dados) são testáveis sem PostgreSQL. Editar, remover responsável e excluir
/// não usam transação explícita e são exercitados por inteiro.
/// </summary>
public sealed class TaskServiceTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();

    private TaskService Service(CurrentUser? usuario = null) => new(_banco.Contexto, usuario ?? Usuarios.Gestor());

    public void Dispose() => _banco.Dispose();

    private async Task<Tarefa> GravadaAsync(Guid id) =>
        await _banco.Leitura().Tarefas.IgnoreQueryFilters().SingleAsync(tarefa => tarefa.Id == id);

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PorUsuarioComum_Retorna403AntesDaTransacao()
    {
        var projeto = _banco.NovoProjeto();

        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () =>
            Service(Usuarios.Comum("usuario-1")).CriarAsync(new TaskCreateRequest(projeto.Id, "Tarefa", null)));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("projeto-vazio")]
    [InlineData("titulo-em-branco")]
    [InlineData("titulo-longo-demais")]
    [InlineData("descricao-longa-demais")]
    public async Task Criar_ComDadosInvalidos_Retorna400AntesDaTransacao(string caso)
    {
        var projetoId = caso == "projeto-vazio" ? Guid.Empty : _banco.NovoProjeto().Id;
        var titulo = caso switch
        {
            "titulo-em-branco" => "   ",
            "titulo-longo-demais" => new string('a', 101),
            _ => "Tarefa",
        };
        var descricao = caso == "descricao-longa-demais" ? new string('a', 256) : null;

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().CriarAsync(new TaskCreateRequest(projetoId, titulo, descricao)));
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task Atualizar_ResponsavelAtualComPapelUsuario_EditaAPropriaTarefa()
    {
        var responsavel = _banco.NovoResponsavel("usuario-1");
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Pendente, responsavel);

        var editada = await Service(Usuarios.Comum("usuario-1")).AtualizarAsync(tarefa.Id, new TaskUpdateRequest("  Título editado ", " ", tarefa.Versao));

        Assert.Equal(("Título editado", (string?)null, 2), (editada.Titulo, editada.Descricao, editada.Versao));
        Assert.Equal(2, (await GravadaAsync(tarefa.Id)).Versao);
    }

    [Theory]
    [Trait("CA", "CA11")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Atualizar_UsuarioQueNaoEhOResponsavel_Retorna403SemGravar(bool tarefaTemOutroResponsavel)
    {
        var outro = tarefaTemOutroResponsavel ? _banco.NovoResponsavel("usuario-2") : null;
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Pendente, outro);

        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () =>
            Service(Usuarios.Comum("usuario-1")).AtualizarAsync(tarefa.Id, new TaskUpdateRequest("Invasão", null, tarefa.Versao)));

        Assert.Equal("Tarefa", (await GravadaAsync(tarefa.Id)).Titulo);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task Atualizar_TarefaConcluida_Retorna409TarefaConcluida()
    {
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Concluida, _banco.NovoResponsavel("usuario-1"));

        await Erros.DeveFalharAsync(409, "TAREFA_CONCLUIDA", () =>
            Service().AtualizarAsync(tarefa.Id, new TaskUpdateRequest("Novo", null, tarefa.Versao)));

        Assert.Equal("Tarefa", (await GravadaAsync(tarefa.Id)).Titulo);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Atualizar_VersaoDesatualizada_Retorna409Concorrencia()
    {
        var tarefa = _banco.NovaTarefa();

        await Erros.DeveFalharAsync(409, "CONCORRENCIA", () =>
            Service().AtualizarAsync(tarefa.Id, new TaskUpdateRequest("Novo", null, tarefa.Versao + 1)));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("titulo-ausente")]
    [InlineData("versao-zero")]
    public async Task Atualizar_ComDadosInvalidos_Retorna400(string caso)
    {
        var tarefa = _banco.NovaTarefa();
        var pedido = caso == "titulo-ausente"
            ? new TaskUpdateRequest(null!, null, tarefa.Versao)
            : new TaskUpdateRequest("Título", null, 0);

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().AtualizarAsync(tarefa.Id, pedido));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Atualizar_TarefaInexistente_Retorna404()
    {
        await Erros.DeveFalharAsync(404, "RECURSO_NAO_ENCONTRADO", () =>
            Service().AtualizarAsync(Guid.NewGuid(), new TaskUpdateRequest("Título", null, 1)));
    }

    [Theory]
    [Trait("CA", "CA18")]
    [InlineData(CategoriaStatus.Pendente)]
    [InlineData(CategoriaStatus.EmAndamento)]
    public async Task Excluir_TarefaNaoConcluida_RegistraExclusaoLogica(CategoriaStatus categoria)
    {
        var tarefa = _banco.NovaTarefa(categoria);

        await Service().ExcluirAsync(tarefa.Id, tarefa.Versao);

        Assert.NotNull((await GravadaAsync(tarefa.Id)).ExcluidoEm);
        Assert.Equal(0, await _banco.Leitura().Tarefas.CountAsync());
    }

    [Fact]
    [Trait("CA", "CA18")]
    [Trait("RN", "RN06")]
    public async Task Excluir_TarefaConcluida_Retorna409TarefaConcluida()
    {
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Concluida, _banco.NovoResponsavel("usuario-1"));

        await Erros.DeveFalharAsync(409, "TAREFA_CONCLUIDA", () => Service().ExcluirAsync(tarefa.Id, tarefa.Versao));

        Assert.Null((await GravadaAsync(tarefa.Id)).ExcluidoEm);
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Excluir_PorUsuarioComum_Retorna403()
    {
        var tarefa = _banco.NovaTarefa();

        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () => Service(Usuarios.Comum("usuario-1")).ExcluirAsync(tarefa.Id, tarefa.Versao));

        Assert.Null((await GravadaAsync(tarefa.Id)).ExcluidoEm);
    }

    [Fact]
    [Trait("CA", "CA30")]
    public async Task Excluir_TarefaJaExcluida_Retorna404()
    {
        var tarefa = _banco.NovaTarefa();
        await Service().ExcluirAsync(tarefa.Id, tarefa.Versao);

        await Erros.DeveFalharAsync(404, "RECURSO_NAO_ENCONTRADO", () => Service().ExcluirAsync(tarefa.Id, tarefa.Versao + 1));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task RemoverResponsavel_DesvinculaEIncrementaAVersao()
    {
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Pendente, _banco.NovoResponsavel("usuario-1"));

        var resultado = await Service().RemoverResponsavelAsync(tarefa.Id, tarefa.Versao);

        Assert.Equal(((Guid?)null, 2), (resultado.ResponsavelId, resultado.Versao));
        Assert.Null((await GravadaAsync(tarefa.Id)).ResponsavelId);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task RemoverResponsavel_DeTarefaConcluida_Retorna409TarefaConcluida()
    {
        var responsavel = _banco.NovoResponsavel("usuario-1");
        var tarefa = _banco.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        await Erros.DeveFalharAsync(409, "TAREFA_CONCLUIDA", () => Service().RemoverResponsavelAsync(tarefa.Id, tarefa.Versao));

        Assert.Equal(responsavel.Id, (await GravadaAsync(tarefa.Id)).ResponsavelId);
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task AtribuirResponsavel_PorUsuarioComum_Retorna403AntesDaTransacao()
    {
        var responsavel = _banco.NovoResponsavel("usuario-1");
        var tarefa = _banco.NovaTarefa();

        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () =>
            Service(Usuarios.Comum("usuario-1")).AtribuirResponsavelAsync(tarefa.Id, new AssignResponsibleRequest(responsavel.Id, tarefa.Versao)));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("atribuir-sem-responsavel")]
    [InlineData("atribuir-versao-zero")]
    [InlineData("status-vazio")]
    [InlineData("status-versao-zero")]
    public async Task OperacoesTransacionais_ComDadosInvalidos_Retornam400AntesDaTransacao(string caso)
    {
        var tarefa = _banco.NovaTarefa();
        var service = Service();
        Func<Task> acao = caso switch
        {
            "atribuir-sem-responsavel" => () => service.AtribuirResponsavelAsync(tarefa.Id, new AssignResponsibleRequest(Guid.Empty, tarefa.Versao)),
            "atribuir-versao-zero" => () => service.AtribuirResponsavelAsync(tarefa.Id, new AssignResponsibleRequest(Guid.NewGuid(), 0)),
            "status-vazio" => () => service.AlterarStatusAsync(tarefa.Id, new ChangeTaskStatusRequest(Guid.Empty, tarefa.Versao)),
            _ => () => service.AlterarStatusAsync(tarefa.Id, new ChangeTaskStatusRequest(BancoEmMemoria.EmAndamentoId, 0)),
        };

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", acao);
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Listar_ComFiltroDeProjetoInexistente_Retorna400()
    {
        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().ListarAsync(1, 20, Guid.NewGuid()));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Listar_DevolveSoAsTarefasVisiveisDoProjeto()
    {
        var projeto = _banco.NovoProjeto();
        var visivel = _banco.NovaTarefa(projeto: projeto);
        _banco.NovaTarefa(projeto: projeto).ExcluidoEm = DateTimeOffset.UtcNow;
        await _banco.Contexto.SaveChangesAsync();
        _banco.NovaTarefa();

        var pagina = await Service().ListarAsync(1, 20, projeto.Id);

        Assert.Equal(visivel.Id, Assert.Single(pagina.Itens).Id);
        Assert.Equal(1, pagina.Total);
    }
}
