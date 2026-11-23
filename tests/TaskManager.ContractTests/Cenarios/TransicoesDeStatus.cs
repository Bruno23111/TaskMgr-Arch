using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Matriz de transições (seção 1), RN02–RN05 e CA03–CA10, CA24, CA25.</summary>
public abstract class TransicoesDeStatus(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA03")]
    public async Task CA03_GestorIniciaTarefaPendenteSemResponsavel()
    {
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        var iniciada = await MudarStatusComSucessoAsync(tarefa, EmAndamento);

        Assert.Equal(EmAndamento.Id, iniciada.StatusId);
        Assert.Null(iniciada.ResponsavelId);
        Assert.Equal(tarefa.Versao + 1, iniciada.Versao);
    }

    [Fact]
    [Trait("CA", "CA03")]
    public async Task CA03_ResponsavelAtualComPapelUsuarioIniciaPropriaTarefa()
    {
        var (usuario, cliente) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, usuario.Id);

        var iniciada = await MudarStatusComSucessoAsync(tarefa, EmAndamento, cliente);

        Assert.Equal(EmAndamento.Id, iniciada.StatusId);
        Assert.Equal(usuario.Id, iniciada.ResponsavelId);
    }

    [Fact]
    [Trait("CA", "CA04")]
    public async Task CA04_TarefaEmAndamentoRetornaAPendente()
    {
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento);

        var pendente = await MudarStatusComSucessoAsync(tarefa, Pendente);

        Assert.Equal(Pendente.Id, pendente.StatusId);
        Assert.Null(pendente.ConcluidaEm);
    }

    [Fact]
    [Trait("CA", "CA05")]
    [Trait("RN", "RN05")]
    public async Task CA05_TarefaEmAndamentoComResponsavelAtivoEhConcluidaComDataUtc()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento, usuario.Id);

        var antes = DateTimeOffset.UtcNow.AddSeconds(-5);
        var concluida = await MudarStatusComSucessoAsync(tarefa, Concluida);
        var depois = DateTimeOffset.UtcNow.AddSeconds(5);

        Assert.Equal(Concluida.Id, concluida.StatusId);
        Assert.NotNull(concluida.ConcluidaEm);
        Assert.Equal(TimeSpan.Zero, concluida.ConcluidaEm!.Value.Offset);
        Assert.InRange(concluida.ConcluidaEm.Value, antes, depois);
        Assert.Equal(concluida, await ObterTarefaAsync(tarefa.Id));
    }

    [Fact]
    [Trait("CA", "CA06")]
    [Trait("RN", "RN03")]
    public async Task CA06_ConclusaoSemResponsavelPeloGestor_Retorna409ResponsavelObrigatorio()
    {
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento);

        await MudarStatusAsync(Gestor, tarefa, Concluida)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ResponsavelObrigatorio);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA06")]
    [Trait("RN", "RN03")]
    public async Task CA06_ConclusaoComResponsavelInativo_Retorna409ResponsavelObrigatorio()
    {
        // O responsável concluiu uma tarefa, foi desativado e a tarefa foi reaberta (seção 3).
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);
        await Gestor.PatchAsync(Rotas.Desativacao(usuario.Id), new { versao = usuario.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);
        tarefa = await MudarStatusComSucessoAsync(tarefa, Pendente);
        tarefa = await MudarStatusComSucessoAsync(tarefa, EmAndamento);

        await MudarStatusAsync(Gestor, tarefa, Concluida)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ResponsavelObrigatorio);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA07")]
    [Trait("RN", "RN02")]
    public async Task CA07_PendenteDiretoParaConcluidaMesmoComResponsavel_Retorna409TransicaoInvalida()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, usuario.Id);

        await MudarStatusAsync(Gestor, tarefa, Concluida)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TransicaoInvalida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA07")]
    [Trait("RN", "RN02")]
    public async Task CA07_MatrizEhValidadaAntesDeRN03_PendenteSemResponsavelParaConcluida()
    {
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        await MudarStatusAsync(Gestor, tarefa, Concluida)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TransicaoInvalida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA08")]
    [Trait("RN", "RN04")]
    public async Task CA08_GestorReabreTarefaConcluida_PreservaResponsavelELimpaConclusao()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        var reaberta = await MudarStatusComSucessoAsync(tarefa, Pendente);

        Assert.Equal(Pendente.Id, reaberta.StatusId);
        Assert.Equal(usuario.Id, reaberta.ResponsavelId);
        Assert.Null(reaberta.ConcluidaEm);
        Assert.Equal(reaberta, await ObterTarefaAsync(tarefa.Id));
    }

    [Fact]
    [Trait("CA", "CA09")]
    [Trait("RN", "RN04")]
    public async Task CA09_ResponsavelComPapelUsuarioTentaReabrir_Retorna403()
    {
        var (usuario, cliente) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await MudarStatusAsync(cliente, tarefa, Pendente)
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA10")]
    [Trait("RN", "RN02")]
    public async Task CA10_GestorTentaConcluidaDiretoParaEmAndamento_Retorna409TransicaoInvalida()
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Concluida, usuario.Id);

        await MudarStatusAsync(Gestor, tarefa, EmAndamento)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TransicaoInvalida);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task CA25_RepetirMesmoStatusIdComVersaoAtual_Retorna200SemAlterarVersao()
    {
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento);

        var resposta = await MudarStatusAsync(Gestor, tarefa, EmAndamento).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

        Assert.Equal(tarefa, resposta);
        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task CA25_RepeticaoIdempotenteAindaVerificaVersao()
    {
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento);

        await MudarStatusAsync(Gestor, tarefa, EmAndamento, versao: tarefa.Versao - 1)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.Concorrencia);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA25")]
    public async Task CA25_RepeticaoIdempotenteAindaVerificaPermissao()
    {
        var (_, cliente) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.EmAndamento);

        await MudarStatusAsync(cliente, tarefa, EmAndamento)
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);
    }

    [Fact]
    [Trait("CA", "CA24")]
    public async Task CA24_RenomearStatusNaoMudaAsRegras()
    {
        // Nome enganoso de propósito: as regras usam a categoria, nunca o nome exibido.
        var renomeado = await Gestor.PutAsync(Rotas.StatusItem(Pendente.Id), new { nome = "Concluída", versao = Pendente.Versao })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK);
        Assert.Equal("Concluída", renomeado.Nome);
        Assert.Equal(Pendente.Codigo, renomeado.Codigo);
        Assert.Equal(Pendente.Categoria, renomeado.Categoria);

        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, usuario.Id);
        await MudarStatusAsync(Gestor, tarefa, Concluida)
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.TransicaoInvalida);
    }

    public static TheoryData<string, string, bool, int, string?> Matriz => new()
    {
        // origem, destino, destino é status adicional?, HTTP esperado, código de erro
        { Categorias.Pendente, Categorias.Pendente, false, 200, null },
        { Categorias.Pendente, Categorias.EmAndamento, false, 200, null },
        { Categorias.Pendente, Categorias.Concluida, false, 409, Codigos.TransicaoInvalida },
        { Categorias.EmAndamento, Categorias.Pendente, false, 200, null },
        { Categorias.EmAndamento, Categorias.EmAndamento, false, 200, null },
        { Categorias.EmAndamento, Categorias.Concluida, false, 200, null },
        { Categorias.Concluida, Categorias.Pendente, false, 200, null },
        { Categorias.Concluida, Categorias.EmAndamento, false, 409, Codigos.TransicaoInvalida },
        { Categorias.Concluida, Categorias.Concluida, false, 200, null },

        // CA24: a matriz se repete com status adicionais criados pelo Gestor.
        { Categorias.Pendente, Categorias.Pendente, true, 200, null },
        { Categorias.Pendente, Categorias.EmAndamento, true, 200, null },
        { Categorias.Pendente, Categorias.Concluida, true, 409, Codigos.TransicaoInvalida },
        { Categorias.EmAndamento, Categorias.Pendente, true, 200, null },
        { Categorias.EmAndamento, Categorias.EmAndamento, true, 200, null },
        { Categorias.EmAndamento, Categorias.Concluida, true, 200, null },
        { Categorias.Concluida, Categorias.Pendente, true, 200, null },
        { Categorias.Concluida, Categorias.EmAndamento, true, 409, Codigos.TransicaoInvalida },
        { Categorias.Concluida, Categorias.Concluida, true, 200, null },
    };

    [Theory]
    [Trait("RN", "RN02")]
    [Trait("CA", "CA24")]
    [MemberData(nameof(Matriz))]
    public async Task Matriz_GestorComResponsavelAtivo(string origem, string destino, bool adicional, int httpEsperado, string? codigo)
    {
        var (usuario, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(origem, usuario.Id);
        var alvo = adicional ? await CriarStatusAdicionalAsync(destino) : DoSeed(destino);

        var resposta = await MudarStatusAsync(Gestor, tarefa, alvo);

        if (codigo is not null)
        {
            await resposta.DeveSerProblemaAsync((HttpStatusCode)httpEsperado, codigo);
            await TarefaNaoMudouAsync(tarefa);
            return;
        }

        var depois = await resposta.DeveRetornarAsync<Tarefa>((HttpStatusCode)httpEsperado);
        Assert.Equal(alvo.Id, depois.StatusId);
        Assert.Equal(usuario.Id, depois.ResponsavelId);

        var mesmoStatus = alvo.Id == tarefa.StatusId;
        Assert.Equal(mesmoStatus ? tarefa.Versao : tarefa.Versao + 1, depois.Versao);

        if (destino != Categorias.Concluida)
        {
            Assert.Null(depois.ConcluidaEm);
        }
        else if (origem == Categorias.Concluida)
        {
            // RN05: mudar entre status da categoria concluída preserva a data.
            Assert.Equal(Tarefa.Microssegundos(tarefa.ConcluidaEm), Tarefa.Microssegundos(depois.ConcluidaEm));
        }
        else
        {
            Assert.NotNull(depois.ConcluidaEm);
        }

        Assert.Equal(depois, await ObterTarefaAsync(tarefa.Id));
    }

    private StatusTarefa DoSeed(string categoria) => categoria switch
    {
        Categorias.Pendente => Pendente,
        Categorias.EmAndamento => EmAndamento,
        _ => Concluida,
    };

    private Task<StatusTarefa> CriarStatusAdicionalAsync(string categoria) =>
        Gestor.PostAsync(Rotas.Status, new { codigo = $"ADIC_{Guid.NewGuid().ToString("N")[..8]}".ToUpperInvariant(), nome = $"Adicional {categoria}", categoria })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);
}
