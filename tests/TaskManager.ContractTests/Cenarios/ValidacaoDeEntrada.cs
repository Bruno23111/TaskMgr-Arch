using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Alvos inexistentes, referências inválidas, corpos inválidos e versão: CA29, CA30 e CA32.</summary>
public abstract class ValidacaoDeEntrada(ApiFixture api) : CenarioDeContrato(api)
{
    public static TheoryData<string> AlvosInexistentes => new()
    {
        "obter-projeto", "editar-projeto", "excluir-projeto",
        "obter-tarefa", "editar-tarefa", "excluir-tarefa", "alterar-status", "atribuir-responsavel", "remover-responsavel",
        "obter-status", "editar-status", "excluir-status",
        "obter-responsavel", "editar-responsavel", "desativar-responsavel", "excluir-responsavel",
    };

    [Theory]
    [Trait("CA", "CA29")]
    [MemberData(nameof(AlvosInexistentes))]
    public async Task CA29_AlvoInexistente_Retorna404(string acao)
    {
        var id = Guid.NewGuid();
        var (perfil, _) = await CriarContaAsync();

        var resposta = acao switch
        {
            "obter-projeto" => Gestor.GetAsync(Rotas.Projeto(id)),
            "editar-projeto" => Gestor.PutAsync(Rotas.Projeto(id), new { nome = "X", descricao = (string?)null, versao = 1 }),
            "excluir-projeto" => Gestor.DeleteAsync(Rotas.ProjetoComVersao(id, 1)),
            "obter-tarefa" => Gestor.GetAsync(Rotas.Tarefa(id)),
            "editar-tarefa" => Gestor.PutAsync(Rotas.Tarefa(id), new { titulo = "X", descricao = (string?)null, versao = 1 }),
            "excluir-tarefa" => Gestor.DeleteAsync(Rotas.TarefaComVersao(id, 1)),
            "alterar-status" => Gestor.PatchAsync(Rotas.StatusDaTarefa(id), new { statusId = EmAndamento.Id, versao = 1 }),
            "atribuir-responsavel" => Gestor.PutAsync(Rotas.ResponsavelDaTarefa(id), new { responsavelId = perfil.Id, versao = 1 }),
            "remover-responsavel" => Gestor.DeleteAsync(Rotas.ResponsavelDaTarefaComVersao(id, 1)),
            "obter-status" => Gestor.GetAsync(Rotas.StatusItem(id)),
            "editar-status" => Gestor.PutAsync(Rotas.StatusItem(id), new { nome = "X", versao = 1 }),
            "excluir-status" => Gestor.DeleteAsync(Rotas.StatusComVersao(id, 1)),
            "obter-responsavel" => Gestor.GetAsync(Rotas.Responsavel(id)),
            "editar-responsavel" => Gestor.PutAsync(Rotas.Responsavel(id), new { nome = "X", email = EmailUnico(), papel = Papeis.Usuario, versao = 1 }),
            "desativar-responsavel" => Gestor.PatchAsync(Rotas.Desativacao(id), new { versao = 1 }),
            "excluir-responsavel" => Gestor.DeleteAsync(Rotas.ResponsavelComVersao(id, 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(acao), acao, null),
        };

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    public static TheoryData<string> ReferenciasInexistentes => new() { "projeto-da-nova-tarefa", "responsavel-atribuido", "status-de-destino", "projeto-do-filtro" };

    [Theory]
    [Trait("CA", "CA29")]
    [MemberData(nameof(ReferenciasInexistentes))]
    public async Task CA29_ReferenciaInformadaInexistente_Retorna400(string caso)
    {
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        var resposta = caso switch
        {
            "projeto-da-nova-tarefa" => Gestor.PostAsync(Rotas.Tarefas, new { projetoId = Guid.NewGuid(), titulo = "Órfã" }),
            "responsavel-atribuido" => Gestor.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId = Guid.NewGuid(), versao = tarefa.Versao }),
            "status-de-destino" => Gestor.PatchAsync(Rotas.StatusDaTarefa(tarefa.Id), new { statusId = Guid.NewGuid(), versao = tarefa.Versao }),
            _ => Gestor.GetAsync(Rotas.TarefasDoProjeto(Guid.NewGuid())),
        };

        await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);
        await TarefaNaoMudouAsync(tarefa);
    }

    public static TheoryData<string, string> CorposInvalidosDeTarefa => new()
    {
        { "json-malformado", """{"titulo": """ },
        { "titulo-ausente", """{"descricao":"x","versao":1}""" },
        { "titulo-em-branco", """{"titulo":"   ","versao":1}""" },
        { "titulo-longo-demais", $$"""{"titulo":"{{new string('a', 101)}}","versao":1}""" },
        { "descricao-longa-demais", $$"""{"titulo":"ok","descricao":"{{new string('a', 256)}}","versao":1}""" },
        { "versao-ausente", """{"titulo":"ok"}""" },
        { "versao-zero", """{"titulo":"ok","versao":0}""" },
        { "versao-texto", """{"titulo":"ok","versao":"um"}""" },
        { "campo-desconhecido", """{"titulo":"ok","versao":1,"prioridade":"alta"}""" },
    };

    [Theory]
    [Trait("CA", "CA29")]
    [MemberData(nameof(CorposInvalidosDeTarefa))]
    public async Task CA29_CorpoInvalidoNaEdicaoDeTarefa_Retorna400SemEfeito(string caso, string json)
    {
        _ = caso;
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        await Gestor.EnviarJsonCruAsync(HttpMethod.Put, Rotas.Tarefa(tarefa.Id), json)
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        await TarefaNaoMudouAsync(tarefa);
    }

    public static TheoryData<string, string> CorposInvalidosDeProjeto => new()
    {
        { "nome-ausente", """{"descricao":"x"}""" },
        { "nome-nulo", """{"nome":null}""" },
        { "nome-longo-demais", $$"""{"nome":"{{new string('a', 101)}}"}""" },
        { "corpo-vazio", "" },
        { "array-no-lugar-de-objeto", "[]" },
    };

    [Theory]
    [Trait("CA", "CA29")]
    [MemberData(nameof(CorposInvalidosDeProjeto))]
    public async Task CA29_CorpoInvalidoNaCriacaoDeProjeto_Retorna400SemCriar(string caso, string json)
    {
        _ = caso;

        await Gestor.EnviarJsonCruAsync(HttpMethod.Post, Rotas.Projetos, json)
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        var projetos = await Gestor.GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        Assert.Equal(0, projetos.Total);
    }

    public static TheoryData<string> ExclusoesSemVersao => new() { "projeto", "tarefa", "status", "responsavel", "responsavel-da-tarefa" };

    [Theory]
    [Trait("CA", "CA29")]
    [MemberData(nameof(ExclusoesSemVersao))]
    public async Task CA29_ExclusaoSemVersao_Retorna400(string alvo)
    {
        var (perfil, _) = await CriarContaAsync();
        var projeto = await CriarProjetoAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, perfil.Id, projeto.Id);
        var status = await Gestor.PostAsync(Rotas.Status, new { codigo = "SEM_VERSAO", nome = "Sem versão", categoria = Categorias.Pendente })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);

        var rota = alvo switch
        {
            "projeto" => Rotas.Projeto(projeto.Id),
            "tarefa" => Rotas.Tarefa(tarefa.Id),
            "status" => Rotas.StatusItem(status.Id),
            "responsavel" => Rotas.Responsavel(perfil.Id),
            _ => Rotas.ResponsavelDaTarefa(tarefa.Id),
        };

        await Gestor.DeleteAsync(rota).DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        Assert.Equal(projeto, await ObterProjetoAsync(projeto.Id));
        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task CA29_AutenticacaoEhVerificadaAntesDoCorpo()
    {
        await Anonimo.EnviarJsonCruAsync(HttpMethod.Post, Rotas.Projetos, """{"nome": """)
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);
    }

    [Theory]
    [Trait("CA", "CA30")]
    [InlineData("projeto")]
    [InlineData("tarefa")]
    [InlineData("status")]
    [InlineData("responsavel")]
    public async Task CA30_RepetirExclusaoDeRecursoJaExcluido_Retorna404(string alvo)
    {
        string rota;
        switch (alvo)
        {
            case "projeto":
                var projeto = await CriarProjetoAsync();
                rota = Rotas.ProjetoComVersao(projeto.Id, projeto.Versao);
                break;
            case "tarefa":
                var tarefa = await TarefaEmAsync(Categorias.Pendente);
                rota = Rotas.TarefaComVersao(tarefa.Id, tarefa.Versao);
                break;
            case "status":
                var status = await Gestor.PostAsync(Rotas.Status, new { codigo = "REPETIDA", nome = "Repetida", categoria = Categorias.Pendente })
                    .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);
                rota = Rotas.StatusComVersao(status.Id, status.Versao);
                break;
            default:
                var (perfil, _) = await CriarContaAsync();
                rota = Rotas.ResponsavelComVersao(perfil.Id, perfil.Versao);
                break;
        }

        await Gestor.DeleteAsync(rota).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        await Gestor.DeleteAsync(rota).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Theory]
    [Trait("CA", "CA32")]
    [Trait("RN", "RN07")]
    [InlineData("statusId")]
    [InlineData("responsavelId")]
    [InlineData("projetoId")]
    public async Task CA32_EdicaoGenericaNaoContornaOperacoesEspecificas(string campo)
    {
        var (perfil, _) = await CriarContaAsync();
        var outroProjeto = await CriarProjetoAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente);
        var valor = campo switch
        {
            "statusId" => Concluida.Id,
            "responsavelId" => perfil.Id,
            _ => outroProjeto.Id,
        };

        await Gestor.EnviarJsonCruAsync(
                HttpMethod.Put,
                Rotas.Tarefa(tarefa.Id),
                $$"""{"titulo":"Burlando","versao":{{tarefa.Versao}},"{{campo}}":"{{valor}}"}""")
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);

        await TarefaNaoMudouAsync(tarefa);
    }
}
