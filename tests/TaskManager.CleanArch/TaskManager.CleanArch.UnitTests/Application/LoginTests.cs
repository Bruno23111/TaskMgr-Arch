using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Application.Features.Autenticacao;
using TaskManager.CleanArch.UnitTests.Application.Falsos;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Application;

public sealed class LoginTests
{
    private readonly Ambiente _ambiente = new();
    private readonly TokenServiceFalso _tokens = new();

    private Task<TokenAcesso> EntrarAsync(string email, string senha) =>
        new LoginCommandHandler(_ambiente.Contas, _tokens).Handle(new LoginCommand(email, senha), CancellationToken.None);

    [Fact]
    [Trait("CA", "CA14")]
    public async Task LoginCorreto_GeraTokenComOsPapeisDaConta()
    {
        var usuarioId = _ambiente.Contas.Adicionar("ana@teste.local", Ambiente.Senha, PapeisSistema.Gestor);

        var token = await EntrarAsync("ana@teste.local", Ambiente.Senha);

        Assert.Equal($"token-de-{usuarioId}", token.Valor);
        Assert.Equal((usuarioId, "ana@teste.local"), (_tokens.Ultimo!.UsuarioId, _tokens.Ultimo.Email));
        Assert.Equal([PapeisSistema.Gestor], _tokens.Ultimo.Papeis);
    }

    [Theory]
    [Trait("CA", "CA14")]
    [InlineData("senha-errada")]
    [InlineData("conta-inexistente")]
    [InlineData("conta-desativada")]
    public async Task LoginRecusado_LancaSempreAMesmaExcecaoSemGerarToken(string caso)
    {
        _ambiente.Contas.Adicionar("ana@teste.local", Ambiente.Senha, PapeisSistema.Usuario, ativo: caso != "conta-desativada");
        var email = caso == "conta-inexistente" ? "ninguem@teste.local" : "ana@teste.local";
        var senha = caso == "senha-errada" ? "OutraSenha123!" : Ambiente.Senha;

        var erro = await Assert.ThrowsAsync<CredenciaisInvalidasException>(() => EntrarAsync(email, senha));

        Assert.Equal("NAO_AUTENTICADO", erro.Code);
        Assert.Null(_tokens.Ultimo);
    }

    [Theory]
    [Trait("CA", "CA14")]
    [InlineData("ana@teste.local", "")]
    [InlineData("nao-eh-email", "Senha123!")]
    public async Task LoginComDadosInvalidos_LancaArgumentException(string email, string senha)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => EntrarAsync(email, senha));
    }
}
