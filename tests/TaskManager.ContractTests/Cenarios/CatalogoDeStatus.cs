using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Status como catálogo: CA23, proteção do seed e CA33 (código duplicado).</summary>
public abstract class CatalogoDeStatus(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    public async Task Seed_TemTresStatusProtegidosUmPorCategoria()
    {
        var status = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);

        Assert.Equal(3, status.Count);
        Assert.All(status, s => Assert.True(s.Protegido));
        Assert.All(status, s => Assert.Equal(s.Codigo, s.Categoria));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task CA23_ExcluirStatusAdicionalLivre_Retorna204()
    {
        var status = await CriarStatusAsync("LIVRE", Categorias.Pendente);

        await Gestor.DeleteAsync(Rotas.StatusComVersao(status.Id, status.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);

        await Gestor.GetAsync(Rotas.StatusItem(status.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Theory]
    [Trait("CA", "CA23")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CA23_ExcluirStatusReferenciado_Retorna409RegistroEmUso(bool tarefaExcluida)
    {
        var status = await CriarStatusAsync("EM_REVISAO", Categorias.EmAndamento);
        var tarefa = await TarefaEmAsync(Categorias.Pendente);
        tarefa = await MudarStatusComSucessoAsync(tarefa, status);
        if (tarefaExcluida)
        {
            await Gestor.DeleteAsync(Rotas.TarefaComVersao(tarefa.Id, tarefa.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        }

        await Gestor.DeleteAsync(Rotas.StatusComVersao(status.Id, status.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroEmUso);

        Assert.Equal(status, await Gestor.GetAsync(Rotas.StatusItem(status.Id)).DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK));
        Assert.Equal(status.Id, await Api.ConsultarEscalarAsync<Guid>(
            """SELECT "StatusId" FROM "Tarefas" WHERE "Id" = @id""", ("id", tarefa.Id)));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public async Task CA23_ExcluirStatusProtegidoMesmoSemTarefas_Retorna409RegistroProtegido()
    {
        foreach (var protegido in new[] { Pendente, EmAndamento, Concluida })
        {
            await Gestor.DeleteAsync(Rotas.StatusComVersao(protegido.Id, protegido.Versao))
                .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroProtegido);
        }

        var status = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);
        Assert.Equal(3, status.Count);
    }

    [Fact]
    [Trait("CA", "CA24")]
    public async Task CA24_GestorRenomeiaStatusProtegido_CodigoECategoriaNaoMudam()
    {
        var renomeado = await Gestor.PutAsync(Rotas.StatusItem(EmAndamento.Id), new { nome = "Fazendo", versao = EmAndamento.Versao })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK);

        Assert.Equal(EmAndamento with { Nome = "Fazendo", Versao = EmAndamento.Versao + 1 }, renomeado);
    }

    [Theory]
    [InlineData("codigo", "\"OUTRO\"")]
    [InlineData("categoria", "\"CONCLUIDA\"")]
    public async Task EdicaoDeStatusNaoAceitaAlterarCodigoOuCategoria(string campo, string valor)
    {
        await Gestor.EnviarJsonCruAsync(
                HttpMethod.Put,
                Rotas.StatusItem(Pendente.Id),
                $$"""{"nome":"Pendente","versao":{{Pendente.Versao}},"{{campo}}":{{valor}}}""")
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        Assert.Equal(Pendente, await Gestor.GetAsync(Rotas.StatusItem(Pendente.Id)).DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK));
    }

    [Fact]
    [Trait("CA", "CA33")]
    public async Task CA33_CodigoDeStatusDuplicado_Retorna409SemCriar()
    {
        await Gestor.PostAsync(Rotas.Status, new { codigo = Pendente.Codigo, nome = "Duplicado", categoria = Categorias.Pendente })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ValorDuplicado);

        var status = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);
        Assert.Equal(3, status.Count);
    }

    [Fact]
    public async Task CategoriaNumericaEhRejeitada()
    {
        await Gestor.EnviarJsonCruAsync(HttpMethod.Post, Rotas.Status, """{"codigo":"NUMERICA","nome":"Numérica","categoria":0}""")
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);
    }

    private Task<StatusTarefa> CriarStatusAsync(string codigo, string categoria) =>
        Gestor.PostAsync(Rotas.Status, new { codigo, nome = $"Status {codigo}", categoria })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);
}
