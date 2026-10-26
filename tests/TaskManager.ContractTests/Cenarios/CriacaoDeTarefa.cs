using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

public abstract class CriacaoDeTarefa(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA01")]
    public async Task CA01_GestorCriaTarefaSemResponsavel_IniciaPendenteNaVersao1()
    {
        var projeto = await CriarProjetoAsync();

        var resposta = await Gestor.PostAsync(Rotas.Tarefas, new { projetoId = projeto.Id, titulo = "Escrever testes", descricao = (string?)null });
        var tarefa = await resposta.DeveRetornarAsync<Tarefa>(HttpStatusCode.Created);

        Assert.Equal(projeto.Id, tarefa.ProjetoId);
        Assert.Equal(Pendente.Id, tarefa.StatusId);
        Assert.Equal(1, tarefa.Versao);
        Assert.Null(tarefa.ConcluidaEm);
        Assert.Null(tarefa.ResponsavelId);
        Assert.Equal("Escrever testes", tarefa.Titulo);

        Assert.NotNull(resposta.Headers.Location);
        var persistida = await Gestor.GetAsync(resposta.Headers.Location!.ToString()).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);
        Assert.Equal(tarefa, persistida);
    }

    [Theory]
    [Trait("CA", "CA02")]
    [InlineData(Categorias.EmAndamento)]
    [InlineData(Categorias.Concluida)]
    public async Task CA02_ClienteTentaDefinirStatusNaCriacao_Rejeita400SemCriar(string categoria)
    {
        var projeto = await CriarProjetoAsync();
        var status = categoria == Categorias.Concluida ? Concluida : EmAndamento;

        await Gestor.EnviarJsonCruAsync(
                HttpMethod.Post,
                Rotas.Tarefas,
                $$"""{"projetoId":"{{projeto.Id}}","titulo":"Tarefa","statusId":"{{status.Id}}"}""")
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        Assert.Equal(0, (await ListarTarefasDoProjetoAsync(projeto.Id)).Total);
    }
}
