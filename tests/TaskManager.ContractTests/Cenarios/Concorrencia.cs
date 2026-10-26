using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>
/// CA26–CA28. As requisições disputantes são liberadas juntas por uma largada explícita
/// (sem atrasos arbitrários) e repetidas várias vezes para exercitar ordens diferentes.
/// <para>
/// CA27 e CA28 exigem um resultado serializável, mas não fixam o código de quem perde a disputa:
/// aceita-se o código de negócio da situação ou 409 CONCORRENCIA (conflito de gravação, seção 4).
/// Erro interno (5xx) nunca é aceito. O que se verifica de fato são os invariantes do estado final.
/// </para>
/// </summary>
public abstract class Concorrencia(ApiFixture api) : CenarioDeContrato(api)
{
    private const int Rodadas = 10;

    private static readonly Esperado ConflitoDeGravacao = new(HttpStatusCode.Conflict, Codigos.Concorrencia);

    [Fact]
    [Trait("CA", "CA26")]
    public async Task CA26_VersaoDesatualizada_Retorna409SemGravar()
    {
        var projeto = await CriarProjetoAsync();
        var atual = await Gestor.PutAsync(Rotas.Projeto(projeto.Id), new { nome = "Primeira edição", descricao = (string?)null, versao = projeto.Versao })
            .DeveRetornarAsync<Projeto>(HttpStatusCode.OK);

        await Gestor.PutAsync(Rotas.Projeto(projeto.Id), new { nome = "Edição atrasada", descricao = (string?)null, versao = projeto.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.Concorrencia);

        Assert.Equal(atual, await ObterProjetoAsync(projeto.Id));
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task CA26_DuasEdicoesSimultaneasDeProjetoComAMesmaVersao_ApenasUmaGrava()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var projeto = await CriarProjetoAsync();

            var (a, b) = await SimultaneamenteAsync(
                () => Gestor.PutAsync(Rotas.Projeto(projeto.Id), new { nome = "Edição A", descricao = (string?)null, versao = projeto.Versao }),
                () => Gestor.PutAsync(Rotas.Projeto(projeto.Id), new { nome = "Edição B", descricao = (string?)null, versao = projeto.Versao }));

            var vencedora = await ApenasUmaVenceAsync<Projeto>(a, b);
            Assert.Equal(vencedora, await ObterProjetoAsync(projeto.Id));
            Assert.Equal(projeto.Versao + 1, vencedora.Versao);
        }
    }

    [Fact]
    [Trait("CA", "CA26")]
    public async Task CA26_DuasMudancasSimultaneasDeStatusComAMesmaVersao_ApenasUmaGrava()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var tarefa = await TarefaEmAsync(Categorias.EmAndamento);
            var extra = await Gestor.PostAsync(Rotas.Status, new { codigo = $"EXTRA_{rodada}", nome = "Extra", categoria = Categorias.EmAndamento })
                .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);

            var (a, b) = await SimultaneamenteAsync(
                () => MudarStatusAsync(Gestor, tarefa, Pendente),
                () => MudarStatusAsync(Gestor, tarefa, extra));

            var vencedora = await ApenasUmaVenceAsync<Tarefa>(a, b);
            Assert.Equal(vencedora, await ObterTarefaAsync(tarefa.Id));
            Assert.Equal(tarefa.Versao + 1, vencedora.Versao);
        }
    }

    [Fact]
    [Trait("CA", "CA27")]
    public async Task CA27_ExclusaoDeProjetoDisputaComInicioDeTarefa_NuncaDeixaTarefaVisivelOrfa()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var projeto = await CriarProjetoAsync();
            var tarefa = await CriarTarefaAsync(projeto.Id);

            var (inicio, exclusao) = await SimultaneamenteAsync(
                () => MudarStatusAsync(Gestor, tarefa, EmAndamento),
                () => Gestor.DeleteAsync(Rotas.ProjetoComVersao(projeto.Id, projeto.Versao)));

            var iniciou = await ResultadoAsync(inicio, HttpStatusCode.OK, new(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado), ConflitoDeGravacao);
            var excluiu = await ResultadoAsync(exclusao, HttpStatusCode.NoContent, new(HttpStatusCode.Conflict, Codigos.ProjetoComTarefaEmAndamento), ConflitoDeGravacao);

            Assert.False(iniciou && excluiu, "Início e exclusão não podem vencer juntos: a tarefa em andamento bloqueia a exclusão (RN01).");
            if (iniciou)
            {
                Assert.Equal(EmAndamento.Id, (await ObterTarefaAsync(tarefa.Id)).StatusId);
            }

            if (excluiu)
            {
                await Gestor.GetAsync(Rotas.Tarefa(tarefa.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
            }

            await NenhumaTarefaVisivelEmProjetoExcluidoAsync();
        }
    }

    [Fact]
    [Trait("CA", "CA27")]
    public async Task CA27_ExclusaoDeProjetoDisputaComCriacaoDeTarefa_NuncaDeixaTarefaVisivelOrfa()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var projeto = await CriarProjetoAsync();

            var (criacao, exclusao) = await SimultaneamenteAsync(
                () => Gestor.PostAsync(Rotas.Tarefas, new { projetoId = projeto.Id, titulo = "Na disputa" }),
                () => Gestor.DeleteAsync(Rotas.ProjetoComVersao(projeto.Id, projeto.Versao)));

            var criou = await ResultadoAsync(criacao, HttpStatusCode.Created, new(HttpStatusCode.BadRequest, Codigos.DadosInvalidos), ConflitoDeGravacao);
            var excluiu = await ResultadoAsync(exclusao, HttpStatusCode.NoContent, ConflitoDeGravacao);

            // Tarefa pendente não bloqueia a exclusão: se as duas venceram, a criação veio antes
            // e a tarefa nova tem de ter sido excluída junto com o projeto.
            if (criou && excluiu)
            {
                var criada = await criacao.DeveRetornarAsync<Tarefa>(HttpStatusCode.Created);
                await Gestor.GetAsync(Rotas.Tarefa(criada.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
            }

            await NenhumaTarefaVisivelEmProjetoExcluidoAsync();
        }
    }

    [Fact]
    [Trait("CA", "CA28")]
    public async Task CA28_DesativacaoDeResponsavelDisputaComAtribuicao_NuncaDeixaInativoComTarefaAtiva()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var (perfil, _) = await CriarContaAsync();
            var tarefa = await TarefaEmAsync(Categorias.Pendente);

            var (desativacao, atribuicao) = await SimultaneamenteAsync(
                () => Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao }),
                () => Gestor.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId = perfil.Id, versao = tarefa.Versao }));

            var desativou = await ResultadoAsync(desativacao, HttpStatusCode.OK, new(HttpStatusCode.Conflict, Codigos.RegistroEmUso), ConflitoDeGravacao);
            var atribuiu = await ResultadoAsync(atribuicao, HttpStatusCode.OK, new(HttpStatusCode.Conflict, Codigos.ResponsavelInativo), ConflitoDeGravacao);

            Assert.False(desativou && atribuiu, "Desativação e atribuição não podem vencer juntas.");
            Assert.Equal(!desativou, (await ObterResponsavelAsync(perfil.Id)).Ativo);
            Assert.Equal(atribuiu ? perfil.Id : null, (await ObterTarefaAsync(tarefa.Id)).ResponsavelId);

            Assert.Equal(0, await Api.ConsultarEscalarAsync<long>("""
                SELECT count(*) FROM "Tarefas" t
                JOIN "Responsaveis" r ON r."Id" = t."ResponsavelId"
                JOIN "AspNetUsers" u ON u."Id" = r."UsuarioId"
                WHERE NOT u."Ativo" AND t."ExcluidoEm" IS NULL AND t."ConcluidaEm" IS NULL
                """));
        }
    }

    [Fact]
    [Trait("CA", "CA28")]
    public async Task CA28_ExclusaoDeResponsavelDisputaComAtribuicao_NuncaAtribuiAContaRemovida()
    {
        for (var rodada = 0; rodada < Rodadas; rodada++)
        {
            var (perfil, _) = await CriarContaAsync();
            var tarefa = await TarefaEmAsync(Categorias.Pendente);

            var (exclusao, atribuicao) = await SimultaneamenteAsync(
                () => Gestor.DeleteAsync(Rotas.ResponsavelComVersao(perfil.Id, perfil.Versao)),
                () => Gestor.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId = perfil.Id, versao = tarefa.Versao }));

            var excluiu = await ResultadoAsync(exclusao, HttpStatusCode.NoContent, new(HttpStatusCode.Conflict, Codigos.RegistroEmUso), ConflitoDeGravacao);
            var atribuiu = await ResultadoAsync(atribuicao, HttpStatusCode.OK, new(HttpStatusCode.BadRequest, Codigos.DadosInvalidos), ConflitoDeGravacao);

            Assert.False(excluiu && atribuiu, "Exclusão e atribuição não podem vencer juntas.");
            Assert.Equal(atribuiu ? perfil.Id : null, (await ObterTarefaAsync(tarefa.Id)).ResponsavelId);
            if (excluiu)
            {
                await Gestor.GetAsync(Rotas.Responsavel(perfil.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
            }
        }
    }

    private sealed record Esperado(HttpStatusCode Status, string Codigo);

    /// <summary>Libera as duas requisições ao mesmo tempo e espera ambas.</summary>
    private static async Task<(HttpResponseMessage A, HttpResponseMessage B)> SimultaneamenteAsync(
        Func<Task<HttpResponseMessage>> a,
        Func<Task<HttpResponseMessage>> b)
    {
        var largada = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tarefaA = Task.Run(async () => { await largada.Task; return await a(); });
        var tarefaB = Task.Run(async () => { await largada.Task; return await b(); });

        largada.SetResult();
        return (await tarefaA, await tarefaB);
    }

    /// <summary>Verdadeiro se a operação venceu; falso se perdeu com uma das falhas permitidas.</summary>
    private static async Task<bool> ResultadoAsync(HttpResponseMessage resposta, HttpStatusCode sucesso, params Esperado[] falhasPermitidas)
    {
        if (resposta.StatusCode == sucesso)
        {
            return true;
        }

        var corpo = await resposta.Content.ReadAsStringAsync();
        var obtido = new Esperado(resposta.StatusCode, CodigoDoProblema(corpo));
        if (!falhasPermitidas.Contains(obtido))
        {
            throw new Xunit.Sdk.XunitException(
                $"{resposta.RequestMessage?.Method} {resposta.RequestMessage?.RequestUri?.PathAndQuery}: " +
                $"HTTP {(int)obtido.Status} {obtido.Codigo} não é resultado aceitável numa disputa. " +
                $"Esperado HTTP {(int)sucesso} ou {string.Join(", ", falhasPermitidas.Select(f => $"{(int)f.Status} {f.Codigo}"))}." +
                $"{Environment.NewLine}{corpo}");
        }

        return false;
    }

    private static string CodigoDoProblema(string corpo)
    {
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(corpo);
            return json.RootElement.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "";
        }
        catch (System.Text.Json.JsonException)
        {
            return "";
        }
    }

    private static async Task<T> ApenasUmaVenceAsync<T>(HttpResponseMessage a, HttpResponseMessage b)
    {
        var (vencedora, perdedora) = a.StatusCode == HttpStatusCode.OK ? (a, b) : (b, a);
        await perdedora.DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.Concorrencia);
        return await vencedora.DeveRetornarAsync<T>(HttpStatusCode.OK);
    }

    private async Task NenhumaTarefaVisivelEmProjetoExcluidoAsync() =>
        Assert.Equal(0, await Api.ConsultarEscalarAsync<long>("""
            SELECT count(*) FROM "Tarefas" t
            JOIN "Projetos" p ON p."Id" = t."ProjetoId"
            WHERE t."ExcluidoEm" IS NULL AND p."ExcluidoEm" IS NOT NULL
            """));
}
