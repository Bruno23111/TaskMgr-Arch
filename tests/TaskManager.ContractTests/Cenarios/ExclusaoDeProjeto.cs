using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>RN01 e seção 3: CA15, CA16 e CA17.</summary>
public abstract class ExclusaoDeProjeto(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA15")]
    [Trait("RN", "RN01")]
    public async Task CA15_ProjetoComTarefaEmAndamento_Retorna409EPreservaTudo()
    {
        var projeto = await CriarProjetoAsync();
        var pendente = await TarefaEmAsync(Categorias.Pendente, projetoId: projeto.Id);
        var emAndamento = await TarefaEmAsync(Categorias.EmAndamento, projetoId: projeto.Id);

        await Gestor.DeleteAsync(Rotas.ProjetoComVersao(projeto.Id, projeto.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ProjetoComTarefaEmAndamento);

        Assert.Equal(projeto, await ObterProjetoAsync(projeto.Id));
        await TarefaNaoMudouAsync(pendente);
        await TarefaNaoMudouAsync(emAndamento);
    }

    public static TheoryData<string> ProjetosExcluiveis => new() { "vazio", "so-pendentes", "pendentes-e-concluidas" };

    [Theory]
    [Trait("CA", "CA16")]
    [MemberData(nameof(ProjetosExcluiveis))]
    public async Task CA16_ProjetoSemTarefaEmAndamento_ExcluiLogicamenteProjetoETarefas(string conteudo)
    {
        var projeto = await CriarProjetoAsync();
        var tarefas = new List<Tarefa>();
        if (conteudo != "vazio")
        {
            tarefas.Add(await TarefaEmAsync(Categorias.Pendente, projetoId: projeto.Id));
            tarefas.Add(await TarefaEmAsync(Categorias.Pendente, projetoId: projeto.Id));
        }

        if (conteudo == "pendentes-e-concluidas")
        {
            // A exclusão em conjunto é a exceção explícita à proibição de excluir tarefa concluída.
            var (usuario, _) = await CriarContaAsync();
            tarefas.Add(await TarefaEmAsync(Categorias.Concluida, usuario.Id, projeto.Id));
        }

        await Gestor.DeleteAsync(Rotas.ProjetoComVersao(projeto.Id, projeto.Versao))
            .DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);

        await Gestor.GetAsync(Rotas.Projeto(projeto.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
        foreach (var tarefa in tarefas)
        {
            await Gestor.GetAsync(Rotas.Tarefa(tarefa.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
        }

        var projetos = await Gestor.GetAsync($"{Rotas.Projetos}?tamanho=100").DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        Assert.DoesNotContain(projetos.Itens, p => p.Id == projeto.Id);
        var todasAsTarefas = await Gestor.GetAsync($"{Rotas.Tarefas}?tamanho=100").DeveRetornarAsync<Paginado<Tarefa>>(HttpStatusCode.OK);
        Assert.Empty(todasAsTarefas.Itens);

        // Exclusão lógica: as linhas continuam no banco, marcadas com ExcluidoEm.
        Assert.NotNull(await Api.ConsultarEscalarAsync<DateTime?>(
            """SELECT "ExcluidoEm" FROM "Projetos" WHERE "Id" = @id""", ("id", projeto.Id)));
        Assert.Equal(tarefas.Count, await Api.ConsultarEscalarAsync<long>(
            """SELECT count(*) FROM "Tarefas" WHERE "ProjetoId" = @id AND "ExcluidoEm" IS NOT NULL""", ("id", projeto.Id)));
    }

    [Fact]
    [Trait("CA", "CA17")]
    public async Task CA17_FalhaDePersistenciaDuranteExclusaoEmConjunto_FazRollbackCompleto()
    {
        var projeto = await CriarProjetoAsync();
        var primeira = await CriarTarefaAsync(projeto.Id, "Primeira");
        var sabotada = await CriarTarefaAsync(projeto.Id, "FALHAR_AO_EXCLUIR");
        var terceira = await CriarTarefaAsync(projeto.Id, "Terceira");

        // Simula uma falha do banco no meio da gravação em conjunto.
        await Api.ExecutarSqlAsync("""
            CREATE FUNCTION falhar_exclusao() RETURNS trigger AS $$
            BEGIN
                RAISE EXCEPTION 'falha simulada pelo teste CA17';
            END $$ LANGUAGE plpgsql;

            CREATE TRIGGER falhar_exclusao BEFORE UPDATE ON "Tarefas"
            FOR EACH ROW WHEN (NEW."Titulo" = 'FALHAR_AO_EXCLUIR' AND NEW."ExcluidoEm" IS NOT NULL)
            EXECUTE FUNCTION falhar_exclusao();
            """);

        var resposta = await Gestor.DeleteAsync(Rotas.ProjetoComVersao(projeto.Id, projeto.Versao));
        Assert.False(resposta.IsSuccessStatusCode, $"A exclusão deveria falhar, mas retornou HTTP {(int)resposta.StatusCode}.");

        await Api.ExecutarSqlAsync("""DROP TRIGGER falhar_exclusao ON "Tarefas"; DROP FUNCTION falhar_exclusao();""");

        Assert.Equal(projeto, await ObterProjetoAsync(projeto.Id));
        await TarefaNaoMudouAsync(primeira);
        await TarefaNaoMudouAsync(sabotada);
        await TarefaNaoMudouAsync(terceira);
        Assert.Equal(0, await Api.ConsultarEscalarAsync<long>(
            """SELECT count(*) FROM "Tarefas" WHERE "ProjetoId" = @id AND "ExcluidoEm" IS NOT NULL""", ("id", projeto.Id)));
    }
}
