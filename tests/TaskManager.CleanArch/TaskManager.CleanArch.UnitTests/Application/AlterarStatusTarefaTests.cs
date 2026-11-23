using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.Features.Tarefas;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.Domain.Events;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

public sealed class AlterarStatusTarefaTests
{
    private readonly Ambiente _ambiente = new();

    private Task<TaskManager.CleanArch.Application.DTOs.TarefaDto> AlterarAsync(
        UsuarioFalso usuario, Guid tarefaId, Guid statusId, int versao) =>
        new AlterarStatusTarefaCommandHandler(
                _ambiente.Tarefas,
                _ambiente.Projetos,
                _ambiente.Status,
                _ambiente.Responsaveis,
                _ambiente.UnidadeDeTrabalho,
                usuario,
                _ambiente.Relogio,
                _ambiente.Despachante)
            .Handle(new AlterarStatusTarefaCommand(tarefaId, statusId, versao), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA05")]
    public async Task Concluir_GravaUmaVezEmTransacaoSerializavelEDespachaOEvento()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.EmAndamento, responsavel);

        var resultado = await AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.Concluida.Id, tarefa.Versao);

        Assert.Equal(_ambiente.Concluida.Id, resultado.StatusId);
        Assert.Equal(_ambiente.Relogio.DataHoraAgoraUTC, resultado.ConcluidaEm);
        Assert.Equal((1, 1), (_ambiente.UnidadeDeTrabalho.Gravacoes, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis));
        var evento = Assert.IsType<TarefaStatusAlteradoDomainEvent>(Assert.Single(_ambiente.Despachante.Despachados));
        Assert.Equal((CategoriaStatus.EmAndamento, CategoriaStatus.Concluida), (evento.CategoriaAnterior, evento.NovaCategoria));
        Assert.Empty(tarefa.DomainEvents);
    }

    [Fact]
    [Trait("CA", "CA03")]
    public async Task ResponsavelAtualComPapelUsuario_IniciaAPropriaTarefa()
    {
        var (responsavel, usuario) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, responsavel);

        var resultado = await AlterarAsync(usuario, tarefa.Id, _ambiente.EmAndamento.Id, tarefa.Versao);

        Assert.Equal(_ambiente.EmAndamento.Id, resultado.StatusId);
    }

    [Fact]
    [Trait("CA", "CA03")]
    public async Task Gestor_IniciaTarefaPendenteSemResponsavel()
    {
        var tarefa = _ambiente.NovaTarefa();

        var resultado = await AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.EmAndamento.Id, tarefa.Versao);

        Assert.Equal(_ambiente.EmAndamento.Id, resultado.StatusId);
    }

    [Fact]
    public async Task UsuarioNaoAutenticado_EhRejeitadoAntesDeQualquerConsulta()
    {
        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            AlterarAsync(UsuarioFalso.Anonimo, Guid.NewGuid(), _ambiente.EmAndamento.Id, 1));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis);
    }

    [Theory]
    [Trait("CA", "CA11")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UsuarioQueNaoEhOResponsavel_RecebeAcessoNegado(bool tarefaTemOutroResponsavel)
    {
        var (_, usuario) = _ambiente.NovaConta();
        var outro = tarefaTemOutroResponsavel ? _ambiente.NovaConta().Perfil : null;
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, outro);

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            AlterarAsync(usuario, tarefa.Id, _ambiente.EmAndamento.Id, tarefa.Versao));

        Assert.Equal(_ambiente.Pendente.Id, tarefa.StatusId);
        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA09")]
    [Trait("RN", "RN04")]
    public async Task ResponsavelComPapelUsuario_TentaReabrir_RecebeAcessoNegadoENaoTransicaoInvalida()
    {
        var (responsavel, usuario) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            AlterarAsync(usuario, tarefa.Id, _ambiente.Pendente.Id, tarefa.Versao));

        Assert.Equal(_ambiente.Concluida.Id, tarefa.StatusId);
    }

    [Fact]
    [Trait("CA", "CA08")]
    public async Task Gestor_ReabreTarefaConcluida()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Concluida, responsavel);

        var resultado = await AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.Pendente.Id, tarefa.Versao);

        Assert.Equal((_ambiente.Pendente.Id, responsavel.Id, (DateTimeOffset?)null),
            (resultado.StatusId, resultado.ResponsavelId!.Value, resultado.ConcluidaEm));
    }

    [Theory]
    [Trait("CA", "CA06")]
    [Trait("RN", "RN03")]
    [InlineData("sem-responsavel")]
    [InlineData("perfil-inativo")]
    public async Task Concluir_SemResponsavelAtivo_ViolaResponsavelObrigatorio(string caso)
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.EmAndamento, caso == "sem-responsavel" ? null : responsavel);
        if (caso == "perfil-inativo")
            _ambiente.Desativar(responsavel);

        await Regras.DeveViolarAsync("RESPONSAVEL_OBRIGATORIO", () =>
            AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.Concluida.Id, tarefa.Versao));

        Assert.Equal(_ambiente.EmAndamento.Id, tarefa.StatusId);
        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
        Assert.Empty(_ambiente.Despachante.Despachados);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task VersaoDesatualizada_LancaConflitoDeConcorrenciaSemGravar()
    {
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() =>
            AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.EmAndamento.Id, tarefa.Versao + 1));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task RepeticaoIdempotente_AindaVerificaAVersao()
    {
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() =>
            AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.Pendente.Id, tarefa.Versao + 1));
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task RepeticaoIdempotente_AindaVerificaAPermissao()
    {
        var (_, usuario) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<AcessoNegadoException>(() =>
            AlterarAsync(usuario, tarefa.Id, _ambiente.Pendente.Id, tarefa.Versao));
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task RepeticaoIdempotente_NaoAlteraVersaoNemDespachaEvento()
    {
        var tarefa = _ambiente.NovaTarefa();

        var resultado = await AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.Pendente.Id, tarefa.Versao);

        Assert.Equal(1, resultado.Versao);
        Assert.Empty(_ambiente.Despachante.Despachados);
    }

    [Fact]
    [Trait("CA", "CA24")]
    public async Task StatusAdicionalRenomeadoDeFormaEnganosa_SegueACategoriaENaoONome()
    {
        var (responsavel, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, responsavel);
        var enganoso = _ambiente.AdicionarStatus("FINALIZADA", CategoriaStatus.EmAndamento);
        enganoso.Renomear("Concluída");

        var resultado = await AlterarAsync(_ambiente.Gestor, tarefa.Id, enganoso.Id, tarefa.Versao);

        Assert.Equal(enganoso.Id, resultado.StatusId);
        Assert.Null(resultado.ConcluidaEm);
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task TarefaInexistente_LancaRecursoNaoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            AlterarAsync(_ambiente.Gestor, Guid.NewGuid(), _ambiente.EmAndamento.Id, 1));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task StatusDeDestinoInexistente_LancaArgumentException()
    {
        var tarefa = _ambiente.NovaTarefa();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            AlterarAsync(_ambiente.Gestor, tarefa.Id, Guid.NewGuid(), tarefa.Versao));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("versao-zero")]
    [InlineData("status-vazio")]
    public async Task DadosInvalidos_SaoRejeitadosAntesDaTransacao(string caso)
    {
        var tarefa = _ambiente.NovaTarefa();
        var statusId = caso == "status-vazio" ? Guid.Empty : _ambiente.EmAndamento.Id;
        var versao = caso == "versao-zero" ? 0 : tarefa.Versao;

        await Assert.ThrowsAsync<ArgumentException>(() => AlterarAsync(_ambiente.Gestor, tarefa.Id, statusId, versao));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis);
    }

    [Fact]
    [Trait("CA", "CA27")]
    public async Task TarefaDeProjetoExcluido_LancaRecursoNaoEncontrado()
    {
        var projeto = _ambiente.NovoProjeto();
        var tarefa = _ambiente.NovaTarefa(projeto: projeto);
        projeto.Excluir(_ambiente.Relogio.DataHoraAgoraUTC);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(() =>
            AlterarAsync(_ambiente.Gestor, tarefa.Id, _ambiente.EmAndamento.Id, tarefa.Versao));
    }
}
