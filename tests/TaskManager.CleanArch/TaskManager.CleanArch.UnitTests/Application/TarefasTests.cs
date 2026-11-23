using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.Features.Tarefas;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

/// <summary>Criação, edição, atribuição, exclusão e consulta de tarefas.</summary>
public sealed class TarefasTests
{
    private readonly Ambiente _ambiente = new();

    private Task<TarefaDto> CriarAsync(UsuarioFalso usuario, Guid projetoId, string titulo = "Nova tarefa", string? descricao = null) =>
        new CriarTarefaCommandHandler(_ambiente.Projetos, _ambiente.Status, _ambiente.Tarefas, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new CriarTarefaCommand(projetoId, titulo, descricao), CancellationToken.None);

    private Task<TarefaDto> AtualizarAsync(UsuarioFalso usuario, Guid tarefaId, int versao, string titulo = "Título editado") =>
        new AtualizarTarefaCommandHandler(_ambiente.Tarefas, _ambiente.Status, _ambiente.Responsaveis, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new AtualizarTarefaCommand(tarefaId, titulo, null, versao), CancellationToken.None);

    private Task<TarefaDto> AtribuirAsync(UsuarioFalso usuario, Guid tarefaId, Guid responsavelId, int versao) =>
        new AtribuirResponsavelTarefaCommandHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Responsaveis, _ambiente.Status, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new AtribuirResponsavelTarefaCommand(tarefaId, responsavelId, versao), CancellationToken.None);

    private Task<TarefaDto> RemoverResponsavelAsync(UsuarioFalso usuario, Guid tarefaId, int versao) =>
        new RemoverResponsavelTarefaCommandHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Status, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new RemoverResponsavelTarefaCommand(tarefaId, versao), CancellationToken.None);

    private Task ExcluirAsync(UsuarioFalso usuario, Guid tarefaId, int versao) =>
        new ExcluirTarefaCommandHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Status, _ambiente.UnidadeDeTrabalho, usuario, _ambiente.Relogio)
            .Handle(new ExcluirTarefaCommand(tarefaId, versao), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA01")]
    public async Task Criar_IniciaNoStatusPendenteInicialNaVersao1()
    {
        var projeto = _ambiente.NovoProjeto();

        var tarefa = await CriarAsync(_ambiente.Gestor, projeto.Id, "  Escrever testes ");

        Assert.Equal((projeto.Id, _ambiente.Pendente.Id, "Escrever testes", 1),
            (tarefa.ProjetoId, tarefa.StatusId, tarefa.Titulo, tarefa.Versao));
        Assert.Null(tarefa.ResponsavelId);
        Assert.Single(_ambiente.Tarefas.Itens);
        Assert.Equal((1, 1), (_ambiente.UnidadeDeTrabalho.Gravacoes, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis));
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PorUsuarioComum_RecebeAcessoNegadoSemCriar()
    {
        var (_, usuario) = _ambiente.NovaConta();
        var projeto = _ambiente.NovoProjeto();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => CriarAsync(usuario, projeto.Id));

        Assert.Empty(_ambiente.Tarefas.Itens);
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PermissaoEhVerificadaAntesDosDados()
    {
        var (_, usuario) = _ambiente.NovaConta();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => CriarAsync(usuario, Guid.Empty, titulo: ""));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("projeto-vazio")]
    [InlineData("projeto-inexistente")]
    [InlineData("projeto-excluido")]
    [InlineData("titulo-em-branco")]
    [InlineData("titulo-longo-demais")]
    public async Task Criar_ComDadosInvalidos_LancaArgumentExceptionSemCriar(string caso)
    {
        var projeto = _ambiente.NovoProjeto();
        if (caso == "projeto-excluido")
            projeto.Excluir(_ambiente.Relogio.DataHoraAgoraUTC);

        var projetoId = caso switch
        {
            "projeto-vazio" => Guid.Empty,
            "projeto-inexistente" => Guid.NewGuid(),
            _ => projeto.Id,
        };
        var titulo = caso switch
        {
            "titulo-em-branco" => "   ",
            "titulo-longo-demais" => new string('a', 101),
            _ => "Tarefa",
        };

        await Assert.ThrowsAsync<ArgumentException>(() => CriarAsync(_ambiente.Gestor, projetoId, titulo));

        Assert.Empty(_ambiente.Tarefas.Itens);
        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task Atualizar_ResponsavelAtualComPapelUsuario_EditaAPropriaTarefa()
    {
        var (responsavel, usuario) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, responsavel);

        var editada = await AtualizarAsync(usuario, tarefa.Id, tarefa.Versao);

        Assert.Equal(("Título editado", 3), (editada.Titulo, editada.Versao));
    }

    [Theory]
    [Trait("CA", "CA11")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Atualizar_UsuarioQueNaoEhOResponsavel_RecebeAcessoNegado(bool tarefaTemOutroResponsavel)
    {
        var (_, usuario) = _ambiente.NovaConta();
        var outro = tarefaTemOutroResponsavel ? _ambiente.NovaConta().Perfil : null;
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, outro);

        await Assert.ThrowsAsync<AcessoNegadoException>(() => AtualizarAsync(usuario, tarefa.Id, tarefa.Versao));

        Assert.Equal("Tarefa", tarefa.Titulo);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task Atualizar_TarefaConcluida_ViolaTarefaConcluida()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        await Regras.DeveViolarAsync("TAREFA_CONCLUIDA", () => AtualizarAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Atualizar_VersaoDesatualizada_LancaConflitoDeConcorrencia()
    {
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() => AtualizarAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao + 1));

        Assert.Equal("Tarefa", tarefa.Titulo);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Atribuir_ResponsavelAtivo_VinculaEIncrementaAVersao()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa();

        var atribuida = await AtribuirAsync(_ambiente.Gestor, tarefa.Id, responsavel.Id, tarefa.Versao);

        Assert.Equal((responsavel.Id, 2), (atribuida.ResponsavelId!.Value, atribuida.Versao));
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task Atribuir_UsuarioComumTentandoSeAtribuir_RecebeAcessoNegado()
    {
        var (perfil, usuario) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => AtribuirAsync(usuario, tarefa.Id, perfil.Id, tarefa.Versao));

        Assert.Null(tarefa.ResponsavelId);
    }

    [Fact]
    [Trait("CA", "CA28")]
    public async Task Atribuir_ResponsavelInativo_ViolaResponsavelInativo()
    {
        var (responsavel, _) = _ambiente.NovaConta(ativo: false);
        var tarefa = _ambiente.NovaTarefa();

        await Regras.DeveViolarAsync("RESPONSAVEL_INATIVO", () => AtribuirAsync(_ambiente.Gestor, tarefa.Id, responsavel.Id, tarefa.Versao));

        Assert.Null(tarefa.ResponsavelId);
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Atribuir_ResponsavelInexistente_LancaArgumentException()
    {
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<ArgumentException>(() => AtribuirAsync(_ambiente.Gestor, tarefa.Id, Guid.NewGuid(), tarefa.Versao));
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData("atribuir")]
    [InlineData("remover")]
    public async Task TrocarOuRemoverResponsavel_DeTarefaConcluida_ViolaTarefaConcluida(string operacao)
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var (outro, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        await Regras.DeveViolarAsync("TAREFA_CONCLUIDA", () => operacao == "atribuir"
            ? AtribuirAsync(_ambiente.Gestor, tarefa.Id, outro.Id, tarefa.Versao)
            : RemoverResponsavelAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao));

        Assert.Equal(responsavel.Id, tarefa.ResponsavelId);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task RemoverResponsavel_DesvinculaEIncrementaAVersao()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, responsavel);

        var resultado = await RemoverResponsavelAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao);

        Assert.Equal(((Guid?)null, 3), (resultado.ResponsavelId, resultado.Versao));
    }

    [Theory]
    [Trait("CA", "CA18")]
    [InlineData(CategoriaStatus.Pendente)]
    [InlineData(CategoriaStatus.EmAndamento)]
    public async Task Excluir_TarefaNaoConcluida_RegistraExclusaoLogicaComAHoraDoRelogio(CategoriaStatus categoria)
    {
        var tarefa = _ambiente.NovaTarefa(categoria);

        await ExcluirAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao);

        Assert.Equal(_ambiente.Relogio.DataHoraAgoraUTC, tarefa.ExcluidoEm);
        Assert.Contains(tarefa, _ambiente.Tarefas.Itens);
    }

    [Fact]
    [Trait("CA", "CA18")]
    [Trait("RN", "RN06")]
    public async Task Excluir_TarefaConcluida_ViolaTarefaConcluida()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        await Regras.DeveViolarAsync("TAREFA_CONCLUIDA", () => ExcluirAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao));

        Assert.Null(tarefa.ExcluidoEm);
    }

    [Fact]
    [Trait("CA", "CA30")]
    public async Task Excluir_TarefaJaExcluida_LancaRecursoNaoEncontrado()
    {
        var tarefa = _ambiente.NovaTarefa();
        await ExcluirAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() => ExcluirAsync(_ambiente.Gestor, tarefa.Id, tarefa.Versao));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Listar_ComFiltroDeProjetoInexistente_LancaArgumentException()
    {
        var handler = new ListarTarefasQueryHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Gestor);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(new ListarTarefasQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_ComPaginacaoInvalida_LancaArgumentOutOfRange(int pagina, int tamanho)
    {
        var handler = new ListarTarefasQueryHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Gestor);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(new ListarTarefasQuery(null, pagina, tamanho), CancellationToken.None));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Listar_DevolveSoAsTarefasVisiveisDoProjeto()
    {
        var projeto = _ambiente.NovoProjeto();
        var visivel = _ambiente.NovaTarefa(projeto: projeto);
        _ambiente.NovaTarefa(projeto: projeto).Excluir(CategoriaStatus.Pendente, _ambiente.Relogio.DataHoraAgoraUTC);
        _ambiente.NovaTarefa();
        var handler = new ListarTarefasQueryHandler(_ambiente.Tarefas, _ambiente.Projetos, _ambiente.Gestor);

        var pagina = await handler.Handle(new ListarTarefasQuery(projeto.Id), CancellationToken.None);

        Assert.Equal(visivel.Id, Assert.Single(pagina.Itens).Id);
        Assert.Equal((1, 20, 1), (pagina.Pagina, pagina.Tamanho, pagina.Total));
    }

    [Fact]
    public async Task Obter_TarefaInexistente_LancaRecursoNaoEncontrado()
    {
        var handler = new ObterTarefaQueryHandler(_ambiente.Tarefas, _ambiente.Gestor);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            handler.Handle(new ObterTarefaQuery(Guid.NewGuid()), CancellationToken.None));
    }
}
