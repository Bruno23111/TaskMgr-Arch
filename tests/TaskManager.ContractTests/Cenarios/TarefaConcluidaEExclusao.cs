using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>CA18 (exclusão individual) e RN06 (tarefa concluída é imutável até reabrir).</summary>
public abstract class TarefaConcluidaEExclusao(ApiFixture api) : CenarioDeContrato(api)
{
    [Theory]
    [Trait("CA", "CA18")]
    [InlineData(Categorias.Pendente)]
    [InlineData(Categorias.EmAndamento)]
    public async Task CA18_GestorExcluiTarefaNaoConcluida_Retorna204(string categoria)
    {
        var tarefa = await TarefaEmAsync(categoria);

        await Gestor.DeleteAsync(Rotas.TarefaComVersao(tarefa.Id, tarefa.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);

        await Gestor.GetAsync(Rotas.Tarefa(tarefa.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
        Assert.NotNull(await Api.ConsultarEscalarAsync<DateTime?>(
            """SELECT "ExcluidoEm" FROM "Tarefas" WHERE "Id" = @id""", ("id", tarefa.Id)));
    }

    [Fact]
    [Trait("CA", "CA18")]
    [Trait("RN", "RN06")]
    public async Task CA18_GestorExcluiTarefaConcluidaIndividualmente_Retorna409()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await Gestor.DeleteAsync(Rotas.TarefaComVersao(tarefa.Id, tarefa.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TarefaConcluida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task RN06_TarefaConcluidaNaoPermiteEditarConteudo()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await Gestor.PutAsync(Rotas.Tarefa(tarefa.Id), new { titulo = "Editada", descricao = (string?)null, versao = tarefa.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TarefaConcluida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task RN06_TarefaConcluidaNaoPermiteTrocarResponsavel()
    {
        var (usuario, _) = await CriarContaAsync();
        var (outro, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await Gestor.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId = outro.Id, versao = tarefa.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TarefaConcluida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task RN06_TarefaConcluidaNaoPermiteRemoverResponsavel()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await Gestor.DeleteAsync(Rotas.ResponsavelDaTarefaComVersao(tarefa.Id, tarefa.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TarefaConcluida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public async Task RN06_AposReabrirAEdicaoVoltaASerPermitida()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);
        tarefa = await MudarStatusComSucessoAsync(tarefa, Pendente);

        var editada = await Gestor.PutAsync(Rotas.Tarefa(tarefa.Id), new { titulo = "Editada após reabrir", descricao = (string?)null, versao = tarefa.Versao })
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

        Assert.Equal("Editada após reabrir", editada.Titulo);
    }
}
