using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Contracts.Status;
using TaskManager.MVC.API.Models;
using TaskManager.MVC.API.Services.Status;
using TaskManager.MVC.UnitTests.Infraestrutura;
using Xunit;

namespace TaskManager.MVC.UnitTests;

/// <summary>
/// StatusService sobre o provedor em memória. A permissão de Gestor fica no atributo [Authorize]
/// do controller, fora do Service; por isso não há teste de 403 aqui.
/// </summary>
public sealed class StatusServiceTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();

    private StatusService Service() => new(_banco.Contexto);

    public void Dispose() => _banco.Dispose();

    [Fact]
    public async Task Seed_TemTresStatusProtegidosUmPorCategoria()
    {
        var status = await Service().ListarAsync();

        Assert.Equal(3, status.Count);
        Assert.All(status, item => Assert.True(item.Protegido));
        Assert.Equal(["CONCLUIDA", "EM_ANDAMENTO", "PENDENTE"], status.Select(item => item.Categoria).Order());
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Criar_StatusAdicional_NormalizaOCodigoEGravaDesprotegido()
    {
        var criado = await Service().CriarAsync(new StatusCreateRequest(" bloqueada ", " Bloqueada ", "PENDENTE"));

        Assert.Equal(("BLOQUEADA", "Bloqueada", "PENDENTE", false, 1),
            (criado.Codigo, criado.Nome, criado.Categoria, criado.Protegido, criado.Versao));
        Assert.Equal(4, await _banco.Leitura().Statuses.CountAsync());
    }

    [Theory]
    [Trait("CA", "CA33")]
    [InlineData("PENDENTE")]
    [InlineData("pendente")]
    public async Task Criar_ComCodigoJaUsado_Retorna409ValorDuplicado(string codigo)
    {
        await Erros.DeveFalharAsync(409, "VALOR_DUPLICADO", () => Service().CriarAsync(new StatusCreateRequest(codigo, "Outro", "PENDENTE")));

        Assert.Equal(3, await _banco.Leitura().Statuses.CountAsync());
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("", "Nome", "PENDENTE")]
    [InlineData("codigo-longo-demais", "Nome", "PENDENTE")]
    [InlineData("NOVO", "nome-longo-demais", "PENDENTE")]
    [InlineData("NOVO", "Nome", "ARQUIVADA")]
    [InlineData("NOVO", "Nome", "1")]
    [InlineData("NOVO", "Nome", "")]
    public async Task Criar_ComDadosInvalidos_Retorna400(string codigo, string nome, string categoria)
    {
        codigo = codigo == "codigo-longo-demais" ? new string('A', 21) : codigo;
        nome = nome == "nome-longo-demais" ? new string('a', 61) : nome;

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().CriarAsync(new StatusCreateRequest(codigo, nome, categoria)));
    }

    [Fact]
    [Trait("CA", "CA24")]
    public async Task Atualizar_StatusProtegido_RenomeiaSemMudarCodigoNemCategoria()
    {
        var atualizado = await Service().AtualizarAsync(BancoEmMemoria.PendenteId, new StatusUpdateRequest("A fazer", 1));

        Assert.Equal(("A fazer", "PENDENTE", "PENDENTE", true, 2),
            (atualizado.Nome, atualizado.Codigo, atualizado.Categoria, atualizado.Protegido, atualizado.Versao));
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Atualizar_VersaoDesatualizada_Retorna409Concorrencia()
    {
        await Erros.DeveFalharAsync(409, "CONCORRENCIA", () =>
            Service().AtualizarAsync(BancoEmMemoria.PendenteId, new StatusUpdateRequest("A fazer", 2)));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task Excluir_StatusProtegido_Retorna409RegistroProtegido()
    {
        await Erros.DeveFalharAsync(409, "REGISTRO_PROTEGIDO", () => Service().ExcluirAsync(BancoEmMemoria.ConcluidaId, 1));

        Assert.Equal(3, await _banco.Leitura().Statuses.CountAsync());
    }

    [Theory]
    [Trait("CA", "CA23")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Excluir_StatusReferenciadoMesmoPorTarefaExcluida_Retorna409RegistroEmUso(bool tarefaExcluida)
    {
        var adicional = await Service().CriarAsync(new StatusCreateRequest("EM_REVISAO", "Em revisão", "EM_ANDAMENTO"));
        var tarefa = _banco.NovaTarefa();
        tarefa.StatusId = adicional.Id;
        tarefa.ExcluidoEm = tarefaExcluida ? DateTimeOffset.UtcNow : null;
        await _banco.Contexto.SaveChangesAsync();

        await Erros.DeveFalharAsync(409, "REGISTRO_EM_USO", () => Service().ExcluirAsync(adicional.Id, adicional.Versao));

        Assert.Equal(4, await _banco.Leitura().Statuses.CountAsync());
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task Excluir_StatusAdicionalLivre_RemoveDoCatalogo()
    {
        var adicional = await Service().CriarAsync(new StatusCreateRequest("BLOQUEADA", "Bloqueada", "PENDENTE"));

        await Service().ExcluirAsync(adicional.Id, adicional.Versao);

        Assert.Null(await Service().ObterAsync(adicional.Id));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("versao-zero")]
    [InlineData("inexistente")]
    public async Task Excluir_SemVersaoOuInexistente_RetornaOErroDoContrato(string caso)
    {
        var (status, codigo, id, versao) = caso == "versao-zero"
            ? (400, "DADOS_INVALIDOS", BancoEmMemoria.PendenteId, 0)
            : (404, "RECURSO_NAO_ENCONTRADO", Guid.NewGuid(), 1);

        await Erros.DeveFalharAsync(status, codigo, () => Service().ExcluirAsync(id, versao));
    }
}
