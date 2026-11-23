using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>CA34: criar, consultar, editar e excluir cada entidade conforme o contrato.</summary>
public abstract class CrudCompleto(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_Projeto()
    {
        var resposta = await Gestor.PostAsync(Rotas.Projetos, new { nome = "Site institucional", descricao = "Novo site" });
        var criado = await resposta.DeveRetornarAsync<Projeto>(HttpStatusCode.Created);
        Assert.Equal(new Projeto(criado.Id, "Site institucional", "Novo site", 1), criado);
        Assert.Equal(criado, await Gestor.GetAsync(resposta.Headers.Location!.ToString()).DeveRetornarAsync<Projeto>(HttpStatusCode.OK));

        var editado = await Gestor.PutAsync(Rotas.Projeto(criado.Id), new { nome = "Site 2.0", descricao = (string?)null, versao = criado.Versao })
            .DeveRetornarAsync<Projeto>(HttpStatusCode.OK);
        Assert.Equal(new Projeto(criado.Id, "Site 2.0", null, 2), editado);
        Assert.Equal(editado, await ObterProjetoAsync(criado.Id));

        await Gestor.DeleteAsync(Rotas.ProjetoComVersao(criado.Id, editado.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        await Gestor.GetAsync(Rotas.Projeto(criado.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_Tarefa()
    {
        var projeto = await CriarProjetoAsync();
        var resposta = await Gestor.PostAsync(Rotas.Tarefas, new { projetoId = projeto.Id, titulo = "Login", descricao = "Tela de login" });
        var criada = await resposta.DeveRetornarAsync<Tarefa>(HttpStatusCode.Created);
        Assert.Equal(criada, await Gestor.GetAsync(resposta.Headers.Location!.ToString()).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK));

        var editada = await Gestor.PutAsync(Rotas.Tarefa(criada.Id), new { titulo = "Login social", descricao = (string?)null, versao = criada.Versao })
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);
        Assert.Equal(criada with { Titulo = "Login social", Descricao = null, Versao = 2 }, editada);

        var lista = await ListarTarefasDoProjetoAsync(projeto.Id);
        Assert.Equal([editada], lista.Itens);

        await Gestor.DeleteAsync(Rotas.TarefaComVersao(criada.Id, editada.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        await Gestor.GetAsync(Rotas.Tarefa(criada.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_AtribuirERemoverResponsavel()
    {
        var (perfil, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        var atribuida = await AtribuirAsync(tarefa, perfil.Id);
        Assert.Equal(tarefa with { ResponsavelId = perfil.Id, Versao = tarefa.Versao + 1 }, atribuida);

        var removida = await Gestor.DeleteAsync(Rotas.ResponsavelDaTarefaComVersao(tarefa.Id, atribuida.Versao))
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);
        Assert.Equal(atribuida with { ResponsavelId = null, Versao = atribuida.Versao + 1 }, removida);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_Status()
    {
        var resposta = await Gestor.PostAsync(Rotas.Status, new { codigo = "BLOQUEADA", nome = "Bloqueada", categoria = Categorias.Pendente });
        var criado = await resposta.DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);
        Assert.Equal(new StatusTarefa(criado.Id, "BLOQUEADA", "Bloqueada", Categorias.Pendente, false, 1), criado);
        Assert.Equal(criado, await Gestor.GetAsync(resposta.Headers.Location!.ToString()).DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK));

        var editado = await Gestor.PutAsync(Rotas.StatusItem(criado.Id), new { nome = "Impedida", versao = criado.Versao })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK);
        Assert.Equal(criado with { Nome = "Impedida", Versao = 2 }, editado);

        var lista = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);
        Assert.Contains(editado, lista);

        await Gestor.DeleteAsync(Rotas.StatusComVersao(criado.Id, editado.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        await Gestor.GetAsync(Rotas.StatusItem(criado.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_ResponsavelEConta()
    {
        var email = EmailUnico();
        var resposta = await Gestor.PostAsync(Rotas.Responsaveis, new { nome = "Ana", email, senha = SenhaPadrao, papel = Papeis.Usuario });
        var criado = await resposta.DeveRetornarAsync<Responsavel>(HttpStatusCode.Created);
        Assert.Equal("Ana", criado.Nome);
        Assert.Equal(email, criado.Email);
        Assert.True(criado.Ativo);
        Assert.Equal(Papeis.Usuario, criado.Papel);
        Assert.Equal(1, criado.Versao);
        Assert.False(string.IsNullOrWhiteSpace(criado.UsuarioId));
        Assert.Equal(criado, await Gestor.GetAsync(resposta.Headers.Location!.ToString()).DeveRetornarAsync<Responsavel>(HttpStatusCode.OK));

        // Usuário comum também consulta responsáveis (seção 2).
        var cliente = await AutenticarAsync(email, SenhaPadrao);
        var lista = await cliente.GetAsync(Rotas.Responsaveis).DeveRetornarAsync<List<Responsavel>>(HttpStatusCode.OK);
        Assert.Contains(criado, lista);

        var novoEmail = EmailUnico();
        var editado = await Gestor.PutAsync(Rotas.Responsavel(criado.Id), new { nome = "Ana Souza", email = novoEmail, papel = Papeis.Gestor, versao = criado.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);
        Assert.Equal(criado with { Nome = "Ana Souza", Email = novoEmail, Papel = Papeis.Gestor, Versao = 2 }, editado);
        await AutenticarAsync(novoEmail, SenhaPadrao);

        await Gestor.DeleteAsync(Rotas.ResponsavelComVersao(criado.Id, editado.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        await Gestor.GetAsync(Rotas.Responsavel(criado.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_PaginacaoDeProjetos()
    {
        foreach (var nome in new[] { "A", "B", "C" })
        {
            await CriarProjetoAsync(nome);
        }

        var primeira = await Gestor.GetAsync($"{Rotas.Projetos}?pagina=1&tamanho=2").DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        var segunda = await Gestor.GetAsync($"{Rotas.Projetos}?pagina=2&tamanho=2").DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        var padrao = await Gestor.GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);

        Assert.Equal((1, 2, 3, 2), (primeira.Pagina, primeira.Tamanho, primeira.Total, primeira.Itens.Count));
        Assert.Equal((2, 2, 3, 1), (segunda.Pagina, segunda.Tamanho, segunda.Total, segunda.Itens.Count));
        Assert.Equal((1, 20), (padrao.Pagina, padrao.Tamanho));
        Assert.Empty(primeira.Itens.Select(p => p.Id).Intersect(segunda.Itens.Select(p => p.Id)));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task CA34_ListasVaziasRetornamColecaoVazia()
    {
        var projetos = await Gestor.GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        var tarefas = await Gestor.GetAsync(Rotas.Tarefas).DeveRetornarAsync<Paginado<Tarefa>>(HttpStatusCode.OK);

        Assert.Empty(projetos.Itens);
        Assert.Equal(0, projetos.Total);
        Assert.Empty(tarefas.Itens);
    }
}
