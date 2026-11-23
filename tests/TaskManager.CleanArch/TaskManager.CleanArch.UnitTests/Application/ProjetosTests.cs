using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.Features.Projetos;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

public sealed class ProjetosTests
{
    private readonly Ambiente _ambiente = new();

    private Task<ProjetoDto> CriarAsync(UsuarioFalso usuario, string nome, string? descricao = null) =>
        new CriarProjetoCommandHandler(_ambiente.Projetos, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new CriarProjetoCommand(nome, descricao), CancellationToken.None);

    private Task<ProjetoDto> AtualizarAsync(Guid projetoId, string nome, int versao) =>
        new AtualizarProjetoCommandHandler(_ambiente.Projetos, _ambiente.UnidadeDeTrabalho, _ambiente.Gestor)
            .Handle(new AtualizarProjetoCommand(projetoId, nome, null, versao), CancellationToken.None);

    private Task ExcluirAsync(UsuarioFalso usuario, Guid projetoId, int versao) =>
        new ExcluirProjetoCommandHandler(_ambiente.Projetos, _ambiente.Tarefas, _ambiente.UnidadeDeTrabalho, usuario, _ambiente.Relogio)
            .Handle(new ExcluirProjetoCommand(projetoId, versao), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Criar_NormalizaEGravaUmaVez()
    {
        var projeto = await CriarAsync(_ambiente.Gestor, "  Site institucional ", "   ");

        Assert.Equal(("Site institucional", (string?)null, 1), (projeto.Nome, projeto.Descricao, projeto.Versao));
        Assert.Single(_ambiente.Projetos.Itens);
        Assert.Equal(1, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PorUsuarioComum_RecebeAcessoNegadoSemCriar()
    {
        var (_, usuario) = _ambiente.NovaConta();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => CriarAsync(usuario, "Projeto"));

        Assert.Empty(_ambiente.Projetos.Itens);
    }

    [Fact]
    [Trait("CA", "CA13")]
    public async Task Criar_SemAutenticacao_RecebeAcessoNegado()
    {
        await Assert.ThrowsAsync<AcessoNegadoException>(() => CriarAsync(UsuarioFalso.Anonimo, "Projeto"));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nome-longo-demais")]
    public async Task Criar_ComNomeInvalido_LancaArgumentException(string nome)
    {
        nome = nome == "nome-longo-demais" ? new string('a', 101) : nome;

        await Assert.ThrowsAsync<ArgumentException>(() => CriarAsync(_ambiente.Gestor, nome));

        Assert.Empty(_ambiente.Projetos.Itens);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Atualizar_VersaoDesatualizada_LancaConflitoSemAlterar()
    {
        var projeto = _ambiente.NovoProjeto("Original");

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() => AtualizarAsync(projeto.Id, "Atrasado", projeto.Versao + 1));

        Assert.Equal(("Original", 1), (projeto.Nome, projeto.Versao));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Atualizar_ProjetoInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => AtualizarAsync(Guid.NewGuid(), "Projeto", 1));
    }

    [Fact]
    [Trait("CA", "CA15")]
    [Trait("RN", "RN01")]
    public async Task Excluir_ComTarefaEmAndamento_LancaProjetoComTarefaEmAndamentoSemExcluirNada()
    {
        var projeto = _ambiente.NovoProjeto();
        var emAndamento = _ambiente.NovaTarefa(CategoriaStatus.EmAndamento, projeto: projeto);
        var pendente = _ambiente.NovaTarefa(projeto: projeto);

        await Assert.ThrowsAsync<ProjetoComTarefaEmAndamentoException>(() => ExcluirAsync(_ambiente.Gestor, projeto.Id, projeto.Versao));

        Assert.Null(projeto.ExcluidoEm);
        Assert.All([emAndamento, pendente], tarefa => Assert.Null(tarefa.ExcluidoEm));
        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA16")]
    public async Task Excluir_SemTarefaEmAndamento_ExcluiProjetoETarefasVisiveisNaMesmaGravacao()
    {
        var projeto = _ambiente.NovoProjeto();
        var (responsavel, _) = _ambiente.NovaConta();
        var pendente = _ambiente.NovaTarefa(projeto: projeto);
        var concluida = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel, projeto);
        var deOutroProjeto = _ambiente.NovaTarefa();

        await ExcluirAsync(_ambiente.Gestor, projeto.Id, projeto.Versao);

        var agora = _ambiente.Relogio.DataHoraAgoraUTC;
        Assert.Equal(agora, projeto.ExcluidoEm);
        Assert.Equal(agora, pendente.ExcluidoEm);
        Assert.Equal(agora, concluida.ExcluidoEm);
        Assert.Null(deOutroProjeto.ExcluidoEm);
        Assert.Equal((1, 1), (_ambiente.UnidadeDeTrabalho.Gravacoes, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis));
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Excluir_PorUsuarioComum_RecebeAcessoNegado()
    {
        var (_, usuario) = _ambiente.NovaConta();
        var projeto = _ambiente.NovoProjeto();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => ExcluirAsync(usuario, projeto.Id, projeto.Versao));

        Assert.Null(projeto.ExcluidoEm);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Excluir_VersaoDesatualizada_LancaConflito()
    {
        var projeto = _ambiente.NovoProjeto();

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() => ExcluirAsync(_ambiente.Gestor, projeto.Id, projeto.Versao + 1));

        Assert.Null(projeto.ExcluidoEm);
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("inexistente")]
    [InlineData("ja-excluido")]
    public async Task Excluir_ProjetoInexistenteOuJaExcluido_LancaRecursoNaoEncontrado(string caso)
    {
        var projeto = _ambiente.NovoProjeto();
        if (caso == "ja-excluido")
            projeto.Excluir(_ambiente.Relogio.DataHoraAgoraUTC);
        var id = caso == "inexistente" ? Guid.NewGuid() : projeto.Id;

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => ExcluirAsync(_ambiente.Gestor, id, projeto.Versao));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Excluir_SemVersaoValida_LancaArgumentExceptionAntesDaTransacao(int versao)
    {
        var projeto = _ambiente.NovoProjeto();

        await Assert.ThrowsAsync<ArgumentException>(() => ExcluirAsync(_ambiente.Gestor, projeto.Id, versao));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Listar_PaginaPorNomeEOcultaExcluidos()
    {
        foreach (var nome in new[] { "C", "A", "B" })
            _ambiente.NovoProjeto(nome);
        _ambiente.NovoProjeto("Excluído").Excluir(_ambiente.Relogio.DataHoraAgoraUTC);
        var handler = new ListarProjetosQueryHandler(_ambiente.Projetos, _ambiente.Gestor);

        var primeira = await handler.Handle(new ListarProjetosQuery(1, 2), CancellationToken.None);
        var segunda = await handler.Handle(new ListarProjetosQuery(2, 2), CancellationToken.None);

        Assert.Equal(["A", "B"], primeira.Itens.Select(projeto => projeto.Nome));
        Assert.Equal(["C"], segunda.Itens.Select(projeto => projeto.Nome));
        Assert.Equal(3, primeira.Total);
    }
}
