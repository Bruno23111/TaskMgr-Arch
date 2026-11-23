using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Application.Features.Responsaveis;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

/// <summary>Contas e perfis: criação, edição de papel, desativação e exclusão.</summary>
public sealed class ResponsaveisTests
{
    private readonly Ambiente _ambiente = new();

    private Task<ResponsavelContaDto> CriarAsync(string email, string papel = PapeisSistema.Usuario) =>
        new CriarResponsavelCommandHandler(_ambiente.Contas, _ambiente.Responsaveis, _ambiente.UnidadeDeTrabalho, _ambiente.Gestor)
            .Handle(new CriarResponsavelCommand("Ana", email, Ambiente.Senha, papel), CancellationToken.None);

    private Task<ResponsavelContaDto?> AtualizarAsync(UsuarioFalso usuario, Guid id, string papel, int versao) =>
        new AtualizarResponsavelCommandHandler(_ambiente.Contas, _ambiente.Responsaveis, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new AtualizarResponsavelCommand(id, "Nome editado", $"{Guid.NewGuid():N}@teste.local", papel, versao), CancellationToken.None);

    private Task<ResponsavelContaDto?> DesativarAsync(UsuarioFalso usuario, Guid id, int versao) =>
        new DesativarResponsavelCommandHandler(_ambiente.Contas, _ambiente.Responsaveis, _ambiente.Tarefas, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new DesativarResponsavelCommand(id, versao), CancellationToken.None);

    private Task<bool?> ExcluirAsync(UsuarioFalso usuario, Guid id, int versao) =>
        new ExcluirResponsavelCommandHandler(_ambiente.Contas, _ambiente.Responsaveis, _ambiente.Tarefas, _ambiente.UnidadeDeTrabalho, usuario)
            .Handle(new ExcluirResponsavelCommand(id, versao), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Criar_CriaContaEPerfilAtivosNaMesmaTransacao()
    {
        var criado = await CriarAsync("ana@teste.local");

        Assert.Equal(("Ana", "ana@teste.local", PapeisSistema.Usuario, true, 1),
            (criado.Nome, criado.Email, criado.Papel, criado.Ativo, criado.Versao));
        Assert.True(_ambiente.Contas.Existe(criado.UsuarioId));
        Assert.Contains(_ambiente.Responsaveis.Itens, perfil => perfil.Id == criado.Id);
        Assert.Equal((1, 1), (_ambiente.UnidadeDeTrabalho.Gravacoes, _ambiente.UnidadeDeTrabalho.TransacoesSerializaveis));
    }

    [Fact]
    [Trait("CA", "CA33")]
    public async Task Criar_ComEmailDuplicado_NaoCriaPerfil()
    {
        await CriarAsync("ana@teste.local");
        var perfis = _ambiente.Responsaveis.Itens.Count;

        await Assert.ThrowsAsync<ValorDuplicadoException>(() => CriarAsync("ANA@teste.local"));

        Assert.Equal(perfis, _ambiente.Responsaveis.Itens.Count);
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("nao-eh-email", PapeisSistema.Usuario)]
    [InlineData("ana@teste.local", "Administrador")]
    [InlineData("ana@teste.local", "gestor")]
    public async Task Criar_ComEmailOuPapelInvalido_LancaArgumentException(string email, string papel)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CriarAsync(email, papel));

        Assert.Single(_ambiente.Responsaveis.Itens);
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task Atualizar_UltimoGestorAtivoSeRebaixando_ViolaRegistroProtegido()
    {
        await Regras.DeveViolarAsync("REGISTRO_PROTEGIDO", () =>
            AtualizarAsync(_ambiente.Gestor, _ambiente.PerfilDoGestor.Id, PapeisSistema.Usuario, _ambiente.PerfilDoGestor.Versao));

        Assert.Equal(0, _ambiente.UnidadeDeTrabalho.Gravacoes);
    }

    [Fact]
    [Trait("CA", "CA31")]
    public async Task Atualizar_RebaixarOutroGestorQuandoNaoEhOUltimo_EhPermitido()
    {
        var (outroGestor, _) = _ambiente.NovaConta(PapeisSistema.Gestor);

        var atualizado = await AtualizarAsync(_ambiente.Gestor, outroGestor.Id, PapeisSistema.Usuario, outroGestor.Versao);

        Assert.Equal((PapeisSistema.Usuario, 2), (atualizado!.Papel, atualizado.Versao));
        Assert.Equal(1, await _ambiente.Contas.ContarGestoresAtivosAsync());
    }

    [Fact]
    [Trait("CA", "CA12")]
    public async Task Atualizar_UsuarioComumSePromovendo_RecebeAcessoNegado()
    {
        var (perfil, usuario) = _ambiente.NovaConta();

        await Assert.ThrowsAsync<AcessoNegadoException>(() => AtualizarAsync(usuario, perfil.Id, PapeisSistema.Gestor, perfil.Versao));
    }

    [Fact]
    [Trait("CA", "CA29")]
    public async Task Atualizar_ResponsavelInexistente_DevolveNulo()
    {
        Assert.Null(await AtualizarAsync(_ambiente.Gestor, Guid.NewGuid(), PapeisSistema.Usuario, 1));
    }

    [Theory]
    [Trait("CA", "CA21")]
    [InlineData(CategoriaStatus.Pendente)]
    [InlineData(CategoriaStatus.EmAndamento)]
    public async Task Desativar_ComTarefaVisivelNaoConcluida_ViolaRegistroEmUsoEMantemAContaAtiva(CategoriaStatus categoria)
    {
        var (perfil, _) = _ambiente.NovaConta();
        _ambiente.NovaTarefa(categoria, perfil);

        await Regras.DeveViolarAsync("REGISTRO_EM_USO", () => DesativarAsync(_ambiente.Gestor, perfil.Id, perfil.Versao));

        Assert.True(perfil.Ativo);
        Assert.True((await _ambiente.Contas.ObterContaAsync(perfil.UsuarioId))!.Ativo);
    }

    [Fact]
    [Trait("CA", "CA22")]
    public async Task Desativar_ComTarefaSoConcluida_InativaPerfilEConta()
    {
        var (perfil, _) = _ambiente.NovaConta();
        _ambiente.NovaTarefa(CategoriaStatus.Concluida, perfil);

        var desativado = await DesativarAsync(_ambiente.Gestor, perfil.Id, perfil.Versao);

        Assert.False(desativado!.Ativo);
        Assert.Null(await _ambiente.Contas.ObterAtivaAsync(perfil.UsuarioId));
    }

    [Theory]
    [Trait("CA", "CA31")]
    [InlineData("desativar")]
    [InlineData("excluir")]
    public async Task GestorAgindoSobreAPropriaConta_ViolaRegistroProtegido(string operacao)
    {
        var (outroGestor, _) = _ambiente.NovaConta(PapeisSistema.Gestor);
        var perfil = _ambiente.PerfilDoGestor;

        // Mesmo havendo outro Gestor ativo, a própria conta continua protegida.
        await Regras.DeveViolarAsync("REGISTRO_PROTEGIDO", () => operacao == "desativar"
            ? DesativarAsync(_ambiente.Gestor, perfil.Id, perfil.Versao)
            : ExcluirAsync(_ambiente.Gestor, perfil.Id, perfil.Versao));

        Assert.True(_ambiente.Contas.Existe(perfil.UsuarioId));
        Assert.True(outroGestor.Ativo);
    }

    [Theory]
    [Trait("CA", "CA31")]
    [InlineData("desativar")]
    [InlineData("excluir")]
    public async Task UltimoGestorAtivo_NaoPodeSerDesativadoNemExcluidoPorOutraConta(string operacao)
    {
        // Pelo HTTP este estado é inalcançável (conta inativa não autentica); o teste unitário
        // exercita a salvaguarda diretamente: um Gestor inativo não conta como Gestor ativo.
        var ultimoAtivo = _ambiente.PerfilDoGestor;
        var (inativo, gestorInativo) = _ambiente.NovaConta(PapeisSistema.Gestor, ativo: false);

        await Regras.DeveViolarAsync("REGISTRO_PROTEGIDO", () => operacao == "desativar"
            ? DesativarAsync(gestorInativo, ultimoAtivo.Id, ultimoAtivo.Versao)
            : ExcluirAsync(gestorInativo, ultimoAtivo.Id, ultimoAtivo.Versao));

        Assert.True(ultimoAtivo.Ativo);
        Assert.False(inativo.Ativo);
    }

    [Theory]
    [Trait("CA", "CA19")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Excluir_ResponsavelComTarefasMesmoExcluidas_ViolaRegistroEmUsoEPreservaAConta(bool tarefaExcluida)
    {
        var (perfil, _) = _ambiente.NovaConta();
        var tarefa = _ambiente.NovaTarefa(CategoriaStatus.Pendente, perfil);
        if (tarefaExcluida)
            tarefa.Excluir(CategoriaStatus.Pendente, _ambiente.Relogio.DataHoraAgoraUTC);

        await Regras.DeveViolarAsync("REGISTRO_EM_USO", () => ExcluirAsync(_ambiente.Gestor, perfil.Id, perfil.Versao));

        Assert.Contains(perfil, _ambiente.Responsaveis.Itens);
        Assert.True(_ambiente.Contas.Existe(perfil.UsuarioId));
    }

    [Fact]
    [Trait("CA", "CA20")]
    public async Task Excluir_ResponsavelSemTarefas_RemovePerfilEConta()
    {
        var (perfil, _) = _ambiente.NovaConta();

        Assert.True(await ExcluirAsync(_ambiente.Gestor, perfil.Id, perfil.Versao));

        Assert.DoesNotContain(perfil, _ambiente.Responsaveis.Itens);
        Assert.False(_ambiente.Contas.Existe(perfil.UsuarioId));
    }

    [Theory]
    [Trait("CA", "CA26")]
    [InlineData("desativar")]
    [InlineData("excluir")]
    public async Task VersaoDesatualizada_LancaConflitoSemAlterar(string operacao)
    {
        var (perfil, _) = _ambiente.NovaConta();

        await Assert.ThrowsAsync<ConflitoDeConcorrenciaException>(() => operacao == "desativar"
            ? DesativarAsync(_ambiente.Gestor, perfil.Id, perfil.Versao + 1)
            : ExcluirAsync(_ambiente.Gestor, perfil.Id, perfil.Versao + 1));

        Assert.True(perfil.Ativo);
        Assert.Contains(perfil, _ambiente.Responsaveis.Itens);
    }

    [Fact]
    [Trait("CA", "CA34")]
    public async Task Listar_UsuarioComumConsultaOsResponsaveisComDadosDaConta()
    {
        var (perfil, usuario) = _ambiente.NovaConta();
        var handler = new ListarResponsaveisQueryHandler(_ambiente.Contas, _ambiente.Responsaveis, usuario);

        var lista = await handler.Handle(new ListarResponsaveisQuery(), CancellationToken.None);

        Assert.Equal(2, lista.Count);
        Assert.Contains(lista, item => item.Id == perfil.Id && item.Papel == PapeisSistema.Usuario && item.Ativo);
    }
}
