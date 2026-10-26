using System.Net;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Exclusão, desativação e proteção de contas: CA19–CA22, CA31 e CA33 (e-mail).</summary>
public abstract class ResponsaveisEContas(ApiFixture api) : CenarioDeContrato(api)
{
    [Theory]
    [Trait("CA", "CA19")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CA19_ExcluirResponsavelReferenciado_Retorna409EPreservaPerfilContaEVinculos(bool tarefaExcluida)
    {
        var (perfil, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(Categorias.Pendente, perfil.Id);
        if (tarefaExcluida)
        {
            await Gestor.DeleteAsync(Rotas.TarefaComVersao(tarefa.Id, tarefa.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);
        }

        await Gestor.DeleteAsync(Rotas.ResponsavelComVersao(perfil.Id, perfil.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroEmUso);

        Assert.Equal(perfil, await ObterResponsavelAsync(perfil.Id));
        await AutenticarAsync(perfil.Email, SenhaPadrao);
        Assert.Equal(perfil.Id, await Api.ConsultarEscalarAsync<Guid>(
            """SELECT "ResponsavelId" FROM "Tarefas" WHERE "Id" = @id""", ("id", tarefa.Id)));
    }

    [Fact]
    [Trait("CA", "CA20")]
    public async Task CA20_ExcluirResponsavelSemReferencia_RemovePerfilEConta()
    {
        var (perfil, _) = await CriarContaAsync();

        await Gestor.DeleteAsync(Rotas.ResponsavelComVersao(perfil.Id, perfil.Versao)).DeveRetornarSemCorpoAsync(HttpStatusCode.NoContent);

        await Gestor.GetAsync(Rotas.Responsavel(perfil.Id)).DeveSerProblemaAsync(HttpStatusCode.NotFound, Codigos.RecursoNaoEncontrado);
        await Anonimo.PostAsync(Rotas.Login, new { email = perfil.Email, senha = SenhaPadrao })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);

        // A conta saiu junto: o mesmo e-mail pode ser cadastrado de novo.
        await Gestor.PostAsync(Rotas.Responsaveis, new { nome = "Recriado", email = perfil.Email, senha = SenhaPadrao, papel = Papeis.Usuario })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.Created);
    }

    [Theory]
    [Trait("CA", "CA21")]
    [InlineData(Categorias.Pendente)]
    [InlineData(Categorias.EmAndamento)]
    public async Task CA21_DesativarResponsavelComTarefaVisivelNaoConcluida_Retorna409AteReatribuir(string categoria)
    {
        var (perfil, _) = await CriarContaAsync();
        var tarefa = await TarefaEmAsync(categoria, perfil.Id);

        await Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroEmUso);
        Assert.Equal(perfil, await ObterResponsavelAsync(perfil.Id));

        await Gestor.DeleteAsync(Rotas.ResponsavelDaTarefaComVersao(tarefa.Id, tarefa.Versao)).DeveRetornarAsync<Tarefa>(HttpStatusCode.OK);

        var desativado = await Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);
        Assert.False(desativado.Ativo);
    }

    [Fact]
    [Trait("CA", "CA22")]
    public async Task CA22_ResponsavelDesativadoComTarefaConcluida_PreservaHistoricoEBloqueiaAcesso()
    {
        var (perfil, cliente) = await CriarContaAsync();
        var concluida = await TarefaEmAsync(Categorias.Concluida, perfil.Id);

        var desativado = await Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);
        Assert.False(desativado.Ativo);

        await TarefaNaoMudouAsync(concluida);
        await Anonimo.PostAsync(Rotas.Login, new { email = perfil.Email, senha = SenhaPadrao })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);
        await cliente.GetAsync(Rotas.Projetos).DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);

        var nova = await TarefaEmAsync(Categorias.Pendente);
        await Gestor.PutAsync(Rotas.ResponsavelDaTarefa(nova.Id), new { responsavelId = perfil.Id, versao = nova.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ResponsavelInativo);
        await TarefaNaoMudouAsync(nova);
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task CA31_GestorTentaExcluirAPropriaConta_Retorna409()
    {
        // Há outro Gestor ativo: a proteção vale para a própria conta, não só para o último Gestor.
        await CriarContaAsync(Papeis.Gestor);
        var eu = await PerfilDoGestorInicialAsync();

        await Gestor.DeleteAsync(Rotas.ResponsavelComVersao(eu.Id, eu.Versao))
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroProtegido);

        Assert.Equal(eu, await ObterResponsavelAsync(eu.Id));
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task CA31_GestorTentaDesativarAPropriaConta_Retorna409()
    {
        await CriarContaAsync(Papeis.Gestor);
        var eu = await PerfilDoGestorInicialAsync();

        await Gestor.PatchAsync(Rotas.Desativacao(eu.Id), new { versao = eu.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroProtegido);

        Assert.Equal(eu, await ObterResponsavelAsync(eu.Id));
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task CA31_UltimoGestorAtivoTentaSeRebaixar_Retorna409()
    {
        var eu = await PerfilDoGestorInicialAsync();

        await Gestor.PutAsync(Rotas.Responsavel(eu.Id), new { nome = eu.Nome, email = eu.Email, papel = Papeis.Usuario, versao = eu.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.RegistroProtegido);

        Assert.Equal(eu, await ObterResponsavelAsync(eu.Id));
        await Gestor.PostAsync(Rotas.Projetos, new { nome = "Ainda sou Gestor" }).DeveRetornarAsync<Projeto>(HttpStatusCode.Created);
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task CA31_Controle_GestorRebaixaOutroGestorQuandoNaoEhOUltimo()
    {
        var (outro, _) = await CriarContaAsync(Papeis.Gestor);

        var rebaixado = await Gestor.PutAsync(Rotas.Responsavel(outro.Id), new { nome = outro.Nome, email = outro.Email, papel = Papeis.Usuario, versao = outro.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);

        Assert.Equal(Papeis.Usuario, rebaixado.Papel);
    }

    [Fact]
    [Trait("CA", "CA33")]
    public async Task CA33_CriarContaComEmailDuplicado_Retorna409SemRegistrosParciais()
    {
        var (existente, _) = await CriarContaAsync();
        var antes = await Gestor.GetAsync(Rotas.Responsaveis).DeveRetornarAsync<List<Responsavel>>(HttpStatusCode.OK);

        await Gestor.PostAsync(Rotas.Responsaveis, new { nome = "Duplicado", email = existente.Email.ToUpperInvariant(), senha = SenhaPadrao, papel = Papeis.Usuario })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ValorDuplicado);

        var depois = await Gestor.GetAsync(Rotas.Responsaveis).DeveRetornarAsync<List<Responsavel>>(HttpStatusCode.OK);
        Assert.Equal(antes.OrderBy(r => r.Id), depois.OrderBy(r => r.Id));
        Assert.Equal(antes.Count, await Api.ConsultarEscalarAsync<long>("""SELECT count(*) FROM "AspNetUsers" """));
    }

    [Fact]
    [Trait("CA", "CA33")]
    public async Task CA33_EditarContaParaEmailDuplicado_Retorna409()
    {
        var (existente, _) = await CriarContaAsync();
        var (alvo, _) = await CriarContaAsync();

        await Gestor.PutAsync(Rotas.Responsavel(alvo.Id), new { nome = alvo.Nome, email = existente.Email, papel = alvo.Papel, versao = alvo.Versao })
            .DeveSerProblemaAsync(HttpStatusCode.Conflict, Codigos.ValorDuplicado);

        Assert.Equal(alvo, await ObterResponsavelAsync(alvo.Id));
    }
}
