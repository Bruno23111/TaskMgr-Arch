using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Matriz de permissões da seção 2: CA11 e CA12.</summary>
public abstract class Permissoes(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA11")]
    public async Task CA11_UsuarioEditaTarefaAlheia_Retorna403()
    {
        var (outro, _) = await CriarContaAsync();
        var (_, usuario) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, outro.Id);

        await usuario.PutAsync(Rotas.Tarefa(tarefa.Id), new { titulo = "Invasão", descricao = (string?)null, versao = tarefa.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task CA11_UsuarioEditaTarefaSemResponsavel_Retorna403()
    {
        var (_, usuario) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        await usuario.PutAsync(Rotas.Tarefa(tarefa.Id), new { titulo = "Invasão", descricao = (string?)null, versao = tarefa.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Theory]
    [Trait("CA", "CA11")]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CA11_UsuarioAlteraStatusDeTarefaAlheiaOuSemResponsavel_Retorna403(bool temOutroResponsavel)
    {
        var (_, usuario) = await CriarContaAsync();
        Guid? responsavel = temOutroResponsavel ? (await CriarContaAsync()).Perfil.Id : null;
        var tarefa = await TarefaEmAsync(Categorias.Pendente, responsavel);

        await MudarStatusAsync(usuario, tarefa, EmAndamento)
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task CA11_UsuarioInformaOProprioIdParaSeAtribuir_Retorna403()
    {
        var (perfil, usuario) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente);

        await usuario.PutAsync(Rotas.ResponsavelDaTarefa(tarefa.Id), new { responsavelId = perfil.Id, versao = tarefa.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        await TarefaNaoMudouAsync(tarefa);
    }

    [Fact]
    [Trait("CA", "CA11")]
    public async Task CA11_Controle_ResponsavelAtualComPapelUsuarioEditaPropriaTarefa()
    {
        var (perfil, usuario) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, perfil.Id);

        var editada = await usuario.PutAsync(Rotas.Tarefa(tarefa.Id), new { titulo = "Título novo", descricao = "Nova descrição", versao = tarefa.Versao })
            .DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

        Assert.Equal("Título novo", editada.Titulo);
        Assert.Equal(tarefa.Versao + 1, editada.Versao);
    }

    public static TheoryData<string> AcoesExclusivasDoGestor => new()
    {
        "criar-projeto", "editar-projeto", "excluir-projeto",
        "criar-tarefa", "excluir-tarefa", "atribuir-responsavel", "remover-responsavel",
        "criar-status", "editar-status", "excluir-status",
        "criar-responsavel", "editar-responsavel", "promover-a-si-mesmo", "desativar-responsavel", "excluir-responsavel",
    };

    [Theory]
    [Trait("CA", "CA12")]
    [MemberData(nameof(AcoesExclusivasDoGestor))]
    public async Task CA12_UsuarioExecutaAcaoExclusivaDoGestor_Retorna403SemEfeito(string acao)
    {
        var alvos = await PrepararAlvosAsync();
        var antes = await FotografarAsync(alvos);

        await ExecutarAsync(alvos, acao, corpoValido: true)
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        Assert.Equal(antes, await FotografarAsync(alvos));
    }

    [Theory]
    [Trait("CA", "CA12")]
    [MemberData(nameof(AcoesExclusivasDoGestor))]
    public async Task CA12_PermissaoGlobalEhVerificadaAntesDoCorpo(string acao)
    {
        // Ordem da seção 4: autenticação, permissão global e só depois o corpo da requisição.
        var alvos = await PrepararAlvosAsync();
        var antes = await FotografarAsync(alvos);

        await ExecutarAsync(alvos, acao, corpoValido: false)
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);

        Assert.Equal(antes, await FotografarAsync(alvos));
    }

    private sealed record Alvos(
        ClienteApi Usuario,
        Responsavel PerfilUsuario,
        Responsavel Outro,
        Projeto Projeto,
        Tarefa Tarefa,
        StatusTarefa Status);

    private sealed record Fotografia(
        Projeto Projeto,
        Tarefa Tarefa,
        StatusTarefa Status,
        Responsavel Outro,
        Responsavel PerfilUsuario,
        int TotalProjetos,
        int TotalTarefas,
        int TotalStatus,
        int TotalResponsaveis);

    private async Task<Alvos> PrepararAlvosAsync()
    {
        var (perfil, usuario) = await CriarContaAsync();
        var (outro, _) = await CriarContaAsync();
        var projeto = await CriarProjetoAsync();

        // O próprio Usuário é o responsável: nem assim ganha ações de Gestor.
        var tarefa = await TarefaEmAsync(Categorias.Pendente, perfil.Id, projeto.Id);
        var status = await Gestor.PostAsync(Rotas.Status, new { codigo = "LIVRE", nome = "Livre", categoria = Categorias.Pendente })
            .DeveRetornarAsync<StatusTarefa>(HttpStatusCode.Created);

        return new Alvos(usuario, perfil, outro, projeto, tarefa, status);
    }

    private async Task<Fotografia> FotografarAsync(Alvos alvos)
    {
        var projetos = await Gestor.GetAsync($"{Rotas.Projetos}?tamanho=100").DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        var tarefas = await Gestor.GetAsync($"{Rotas.Tarefas}?tamanho=100").DeveRetornarAsync<Paginado<Tarefa>>(HttpStatusCode.OK);
        var status = await Gestor.GetAsync(Rotas.Status).DeveRetornarAsync<List<StatusTarefa>>(HttpStatusCode.OK);
        var responsaveis = await Gestor.GetAsync(Rotas.Responsaveis).DeveRetornarAsync<List<Responsavel>>(HttpStatusCode.OK);

        return new Fotografia(
            await ObterProjetoAsync(alvos.Projeto.Id),
            await ObterTarefaAsync(alvos.Tarefa.Id),
            await Gestor.GetAsync(Rotas.StatusItem(alvos.Status.Id)).DeveRetornarAsync<StatusTarefa>(HttpStatusCode.OK),
            await ObterResponsavelAsync(alvos.Outro.Id),
            await ObterResponsavelAsync(alvos.PerfilUsuario.Id),
            projetos.Total,
            tarefas.Total,
            status.Count,
            responsaveis.Count);
    }

    private Task<HttpResponseMessage> ExecutarAsync(Alvos a, string acao, bool corpoValido)
    {
        var u = a.Usuario;
        var v = corpoValido;

        // Com corpo inválido: JSON vazio nas operações com corpo e versão ausente nas exclusões.
        Task<HttpResponseMessage> ComCorpo(HttpMethod metodo, string rota, object corpo) =>
            v ? u.EnviarAsync(metodo, rota, corpo) : u.EnviarJsonCruAsync(metodo, rota, "{}");

        return acao switch
        {
            "criar-projeto" => ComCorpo(HttpMethod.Post, Rotas.Projetos, new { nome = "Projeto do usuário" }),
            "editar-projeto" => ComCorpo(HttpMethod.Put, Rotas.Projeto(a.Projeto.Id), new { nome = "Editado", descricao = (string?)null, versao = a.Projeto.Versao }),
            "excluir-projeto" => u.DeleteAsync(v ? Rotas.ProjetoComVersao(a.Projeto.Id, a.Projeto.Versao) : Rotas.Projeto(a.Projeto.Id)),
            "criar-tarefa" => ComCorpo(HttpMethod.Post, Rotas.Tarefas, new { projetoId = a.Projeto.Id, titulo = "Tarefa do usuário" }),
            "excluir-tarefa" => u.DeleteAsync(v ? Rotas.TarefaComVersao(a.Tarefa.Id, a.Tarefa.Versao) : Rotas.Tarefa(a.Tarefa.Id)),
            "atribuir-responsavel" => ComCorpo(HttpMethod.Put, Rotas.ResponsavelDaTarefa(a.Tarefa.Id), new { responsavelId = a.Outro.Id, versao = a.Tarefa.Versao }),
            "remover-responsavel" => u.DeleteAsync(v ? Rotas.ResponsavelDaTarefaComVersao(a.Tarefa.Id, a.Tarefa.Versao) : Rotas.ResponsavelDaTarefa(a.Tarefa.Id)),
            "criar-status" => ComCorpo(HttpMethod.Post, Rotas.Status, new { codigo = "DO_USUARIO", nome = "Do usuário", categoria = Categorias.Pendente }),
            "editar-status" => ComCorpo(HttpMethod.Put, Rotas.StatusItem(a.Status.Id), new { nome = "Editado", versao = a.Status.Versao }),
            "excluir-status" => u.DeleteAsync(v ? Rotas.StatusComVersao(a.Status.Id, a.Status.Versao) : Rotas.StatusItem(a.Status.Id)),
            "criar-responsavel" => ComCorpo(HttpMethod.Post, Rotas.Responsaveis, new { nome = "Novo", email = EmailUnico(), senha = SenhaPadrao, papel = Papeis.Gestor }),
            "editar-responsavel" => ComCorpo(HttpMethod.Put, Rotas.Responsavel(a.Outro.Id), new { nome = "Editado", email = a.Outro.Email, papel = Papeis.Gestor, versao = a.Outro.Versao }),
            "promover-a-si-mesmo" => ComCorpo(HttpMethod.Put, Rotas.Responsavel(a.PerfilUsuario.Id), new { nome = a.PerfilUsuario.Nome, email = a.PerfilUsuario.Email, papel = Papeis.Gestor, versao = a.PerfilUsuario.Versao }),
            "desativar-responsavel" => ComCorpo(HttpMethod.Patch, Rotas.Desativacao(a.Outro.Id), new { versao = a.Outro.Versao }),
            "excluir-responsavel" => u.DeleteAsync(v ? Rotas.ResponsavelComVersao(a.Outro.Id, a.Outro.Versao) : Rotas.Responsavel(a.Outro.Id)),
            _ => throw new ArgumentOutOfRangeException(nameof(acao), acao, "Ação desconhecida."),
        };
    }
}
