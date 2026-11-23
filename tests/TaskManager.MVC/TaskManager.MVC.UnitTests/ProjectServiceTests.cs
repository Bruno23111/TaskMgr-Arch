using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Contracts.Projects;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Services.Projects;
using TaskManager.MVC.UnitTests.Infraestrutura;
using Xunit;

namespace TaskManager.MVC.UnitTests;

/// <summary>
/// ProjectService sobre o provedor em memória. A exclusão de projeto (CA15–CA17) abre transação
/// serializável e bloqueia a linha com SELECT ... FOR UPDATE: só o que vem antes disso é testável aqui.
/// </summary>
public sealed class ProjectServiceTests : IDisposable
{
    private readonly BancoEmMemoria _banco = new();

    private ProjectService Service(CurrentUser? usuario = null) => new(_banco.Contexto, usuario ?? Usuarios.Gestor());

    public void Dispose() => _banco.Dispose();

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Criar_NormalizaOsTextosEGravaNaVersao1()
    {
        var criado = await Service().CriarAsync(new ProjectCreateRequest("  Site institucional ", "   "));

        Assert.Equal(("Site institucional", (string?)null, 1), (criado.Nome, criado.Descricao, criado.Versao));
        Assert.Equal(1, await _banco.Leitura().Projetos.CountAsync());
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Criar_PorUsuarioComum_Retorna403SemGravar()
    {
        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () =>
            Service(Usuarios.Comum("usuario-1")).CriarAsync(new ProjectCreateRequest("Projeto", null)));

        Assert.Equal(0, await _banco.Leitura().Projetos.CountAsync());
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("nome-longo-demais", null)]
    [InlineData("Projeto", "descricao-longa-demais")]
    public async Task Criar_ComDadosInvalidos_Retorna400SemGravar(string nome, string? descricao)
    {
        nome = nome == "nome-longo-demais" ? new string('a', 101) : nome;
        descricao = descricao == "descricao-longa-demais" ? new string('a', 256) : descricao;

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().CriarAsync(new ProjectCreateRequest(nome, descricao)));

        Assert.Equal(0, await _banco.Leitura().Projetos.CountAsync());
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Atualizar_GravaOsDadosEIncrementaAVersao()
    {
        var projeto = _banco.NovoProjeto("Site");

        var editado = await Service().AtualizarAsync(projeto.Id, new ProjectUpdateRequest("Site 2.0", null, projeto.Versao));

        Assert.Equal(("Site 2.0", 2), (editado.Nome, editado.Versao));
        var gravado = await _banco.Leitura().Projetos.SingleAsync();
        Assert.Equal(("Site 2.0", 2), (gravado.Nome, gravado.Versao));
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task Atualizar_VersaoDesatualizada_Retorna409SemGravar()
    {
        var projeto = _banco.NovoProjeto("Original");

        await Erros.DeveFalharAsync(409, "CONCORRENCIA", () =>
            Service().AtualizarAsync(projeto.Id, new ProjectUpdateRequest("Atrasado", null, projeto.Versao + 1)));

        Assert.Equal("Original", (await _banco.Leitura().Projetos.SingleAsync()).Nome);
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Atualizar_SemVersaoValida_Retorna400(int versao)
    {
        var projeto = _banco.NovoProjeto();

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () =>
            Service().AtualizarAsync(projeto.Id, new ProjectUpdateRequest("Projeto", null, versao)));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Atualizar_ProjetoInexistente_Retorna404()
    {
        await Erros.DeveFalharAsync(404, "RECURSO_NAO_ENCONTRADO", () =>
            Service().AtualizarAsync(Guid.NewGuid(), new ProjectUpdateRequest("Projeto", null, 1)));
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Excluir_PorUsuarioComum_Retorna403AntesDaTransacao()
    {
        var projeto = _banco.NovoProjeto();

        await Erros.DeveFalharAsync(403, "ACESSO_NEGADO", () => Service(Usuarios.Comum("usuario-1")).ExcluirAsync(projeto.Id, projeto.Versao));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Excluir_SemVersaoValida_Retorna400AntesDaTransacao()
    {
        var projeto = _banco.NovoProjeto();

        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().ExcluirAsync(projeto.Id, 0));
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_ComPaginacaoInvalida_Retorna400(int pagina, int tamanho)
    {
        await Erros.DeveFalharAsync(400, "DADOS_INVALIDOS", () => Service().ListarAsync(pagina, tamanho));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Listar_PaginaPorNomeEOcultaExcluidos()
    {
        foreach (var nome in new[] { "C", "A", "B" })
            _banco.NovoProjeto(nome);
        _banco.NovoProjeto("Excluído").ExcluidoEm = DateTimeOffset.UtcNow;
        await _banco.Contexto.SaveChangesAsync();

        var primeira = await Service().ListarAsync(1, 2);
        var segunda = await Service().ListarAsync(2, 2);

        Assert.Equal(["A", "B"], primeira.Itens.Select(projeto => projeto.Nome));
        Assert.Equal(["C"], segunda.Itens.Select(projeto => projeto.Nome));
        Assert.Equal(3, primeira.Total);
    }

    [Fact]
    [Trait("CA", "CA30")]
    public async Task Obter_ProjetoExcluido_NaoEhEncontrado()
    {
        var projeto = _banco.NovoProjeto();
        projeto.ExcluidoEm = DateTimeOffset.UtcNow;
        await _banco.Contexto.SaveChangesAsync();

        Assert.Null(await Service().ObterAsync(projeto.Id));
    }
}
