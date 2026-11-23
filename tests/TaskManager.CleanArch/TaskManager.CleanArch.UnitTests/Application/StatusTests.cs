using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.Features.Status;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

public sealed class StatusTests
{
    private readonly Ambiente _ambiente = new();

    private Task<StatusTarefaDto> CriarAsync(string codigo, CategoriaStatus? categoria, UsuarioFalso? usuario = null) =>
        new CriarStatusCommandHandler(_ambiente.Status, _ambiente.UnidadeDeTrabalho, usuario ?? _ambiente.Gestor)
            .Handle(new CriarStatusCommand(codigo, "Nome", categoria), CancellationToken.None);

    private Task ExcluirAsync(Guid statusId, int versao) =>
        new ExcluirStatusCommandHandler(_ambiente.Status, _ambiente.Tarefas, _ambiente.UnidadeDeTrabalho, _ambiente.Gestor)
            .Handle(new ExcluirStatusCommand(statusId, versao), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Criar_StatusAdicional_NormalizaOCodigoEGrava()
    {
        var status = await CriarAsync("bloqueada", CategoriaStatus.Pendente);

        Assert.Equal(("BLOQUEADA", CategoriaStatus.Pendente, false, 1), (status.Codigo, status.Categoria, status.Protegido, status.Versao));
        Assert.Equal(1, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Theory]
    [Trait("CA", "CA33")]
    [InlineData("PENDENTE")]
    [InlineData("pendente")]
    public async Task Criar_ComCodigoJaUsado_LancaValorDuplicadoSemGravar(string codigo)
    {
        await Assert.ThrowsAsync<ValorDuplicadoException>(() => CriarAsync(codigo, CategoriaStatus.Pendente));

        Assert.Equal(3, _ambiente.Status.Itens.Count);
        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Criar_SemCategoria_LancaArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CriarAsync("BLOQUEADA", categoria: null));
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PorUsuarioComum_RecebeAcessoNegado()
    {
        var (_, usuario) = _ambiente.NovaConta();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => CriarAsync("BLOQUEADA", CategoriaStatus.Pendente, usuario));
    }

    [Fact]
    [Trait("CA", "CA24")]
    public async Task Atualizar_StatusProtegido_RenomeiaSemMudarCodigoNemCategoria()
    {
        var handler = new AtualizarStatusCommandHandler(_ambiente.Status, _ambiente.UnidadeDeTrabalho, _ambiente.Gestor);

        var status = await handler.Handle(new AtualizarStatusCommand(_ambiente.Pendente.Id, "A fazer", 1), CancellationToken.None);

        Assert.Equal(("A fazer", "PENDENTE", CategoriaStatus.Pendente, true, 2),
            (status.Nome, status.Codigo, status.Categoria, status.Protegido, status.Versao));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task Excluir_StatusProtegido_ViolaRegistroProtegido()
    {
        await Regras.DeveViolarAsync("REGISTRO_PROTEGIDO", () => ExcluirAsync(_ambiente.Concluida.Id, 1));

        Assert.Contains(_ambiente.Concluida, _ambiente.Status.Itens);
    }

    [Theory]
    [Trait("CA", "CA23")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Excluir_StatusReferenciadoMesmoPorTarefaExcluida_ViolaRegistroEmUso(bool tarefaExcluida)
    {
        var adicional = _ambiente.AdicionarStatus("EM_REVISAO", CategoriaStatus.EmAndamento);
        var tarefa = _ambiente.NovaTarefa();
        tarefa.AlterarStatus(adicional.Id, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, true, true, _ambiente.Relogio.DataHoraAgoraUTC);
        if (tarefaExcluida)
            tarefa.Excluir(CategoriaStatus.EmAndamento, _ambiente.Relogio.DataHoraAgoraUTC);

        await Regras.DeveViolarAsync("REGISTRO_EM_USO", () => ExcluirAsync(adicional.Id, adicional.Versao));

        Assert.Contains(adicional, _ambiente.Status.Itens);
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task Excluir_StatusAdicionalLivre_RemoveDoCatalogo()
    {
        var adicional = _ambiente.AdicionarStatus("BLOQUEADA", CategoriaStatus.Pendente);

        await ExcluirAsync(adicional.Id, adicional.Versao);

        Assert.DoesNotContain(adicional, _ambiente.Status.Itens);
        Assert.Equal(1, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Excluir_VersaoDesatualizada_LancaConflitoSemRemover()
    {
        var adicional = _ambiente.AdicionarStatus("BLOQUEADA", CategoriaStatus.Pendente);

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() => ExcluirAsync(adicional.Id, adicional.Versao + 1));

        Assert.Contains(adicional, _ambiente.Status.Itens);
    }
}
