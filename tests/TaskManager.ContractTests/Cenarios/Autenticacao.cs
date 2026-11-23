using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TaskManager.ContractTests.Cliente;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

namespace TaskManager.ContractTests.Cenarios;

/// <summary>Login e validação do JWT: CA13 e CA14.</summary>
public abstract class Autenticacao(ApiFixture api) : CenarioDeContrato(api)
{
    [Fact]
    [Trait("CA", "CA14")]
    public async Task CA14_LoginCorreto_RetornaTokenValidoPor30Minutos()
    {
        var antes = DateTimeOffset.UtcNow;

        var login = await Anonimo.PostAsync(Rotas.Login, new { email = ApiFixture.GestorEmail, senha = ApiFixture.GestorSenha })
            .DeveRetornarAsync<LoginResposta>(HttpStatusCode.OK);

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.InRange(login.ExpiraEm, antes.AddMinutes(29), DateTimeOffset.UtcNow.AddMinutes(31));
        await Anonimo.ComToken(login.AccessToken).GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("CA", "CA14")]
    public async Task CA14_LoginIncorreto_Retorna401SemRevelarSeAContaExiste()
    {
        var senhaErrada = await Anonimo.PostAsync(Rotas.Login, new { email = ApiFixture.GestorEmail, senha = "SenhaErrada123!" })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);
        var contaInexistente = await Anonimo.PostAsync(Rotas.Login, new { email = EmailUnico(), senha = "SenhaErrada123!" })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);

        Assert.Equal(senhaErrada.Title, contaInexistente.Title);
        Assert.Equal(senhaErrada.Detail, contaInexistente.Detail);
    }

    [Fact]
    [Trait("CA", "CA14")]
    public async Task CA14_LoginDeContaDesativada_Retorna401ComAMesmaMensagem()
    {
        var email = EmailUnico();
        var perfil = await Gestor.PostAsync(Rotas.Responsaveis, new { nome = "Desativada", email, senha = SenhaPadrao, papel = Papeis.Usuario })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.Created);
        await Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao }).DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);

        var desativada = await Anonimo.PostAsync(Rotas.Login, new { email, senha = SenhaPadrao })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);
        var inexistente = await Anonimo.PostAsync(Rotas.Login, new { email = EmailUnico(), senha = SenhaPadrao })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);

        Assert.Equal(inexistente.Detail, desativada.Detail);
    }

    [Fact]
    [Trait("CA", "CA14")]
    public async Task CA14_LoginSemSenha_Retorna400()
    {
        await Anonimo.EnviarJsonCruAsync(HttpMethod.Post, Rotas.Login, $$"""{"email":"{{ApiFixture.GestorEmail}}"}""")
            .DeveSerProblemaAsync(HttpStatusCode.BadRequest, Codigos.DadosInvalidos);
    }

    [Fact]
    [Trait("CA", "CA13")]
    public async Task CA13_Controle_TokenForjadoComAChaveCorretaEhAceito()
    {
        // Garante que os tokens dos testes seguintes falham pelo motivo testado, e não por montagem errada.
        var token = await ForjarTokenDoGestorAsync(expira: DateTime.UtcNow.AddMinutes(10));

        await Anonimo.ComToken(token).GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
    }

    public static TheoryData<string> TokensInvalidos => new() { "ausente", "expirado", "adulterado", "chave-errada", "emissor-errado", "audiencia-errada", "lixo" };

    [Theory]
    [Trait("CA", "CA13")]
    [MemberData(nameof(TokensInvalidos))]
    public async Task CA13_RequisicaoProtegidaComTokenInvalido_Retorna401SemEfeito(string caso)
    {
        var token = caso switch
        {
            "ausente" => null,
            "expirado" => await ForjarTokenDoGestorAsync(expira: DateTime.UtcNow.AddMinutes(-1)),
            "adulterado" => Adulterar(Gestor.Token!),
            "chave-errada" => await ForjarTokenDoGestorAsync(DateTime.UtcNow.AddMinutes(10), chave: "outra-chave-secreta-com-mais-de-32-bytes-0123456789"),
            "emissor-errado" => await ForjarTokenDoGestorAsync(DateTime.UtcNow.AddMinutes(10), emissor: "Outro.Emissor"),
            "audiencia-errada" => await ForjarTokenDoGestorAsync(DateTime.UtcNow.AddMinutes(10), audiencia: "Outra.Audiencia"),
            _ => "nao-eh-um-jwt",
        };

        await Anonimo.ComToken(token).PostAsync(Rotas.Projetos, new { nome = "Não deve existir" })
            .DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);

        var projetos = await Gestor.GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);
        Assert.Equal(0, projetos.Total);
    }

    [Fact]
    [Trait("CA", "CA13")]
    public async Task CA13_TokenEmitidoAntesDaDesativacao_Retorna401()
    {
        var (perfil, cliente) = await CriarContaAsync();
        await cliente.GetAsync(Rotas.Projetos).DeveRetornarAsync<Paginado<Projeto>>(HttpStatusCode.OK);

        await Gestor.PatchAsync(Rotas.Desativacao(perfil.Id), new { versao = perfil.Versao }).DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);

        await cliente.GetAsync(Rotas.Projetos).DeveSerProblemaAsync(HttpStatusCode.Unauthorized, Codigos.NaoAutenticado);
    }

    [Fact]
    [Trait("CA", "CA13")]
    public async Task CA13_PapelAtualDoServidorPrevaleceSobreOToken()
    {
        // Seção 2: alteração de papel tem efeito imediato, mesmo com token emitido antes.
        var (perfil, cliente) = await CriarContaAsync(Papeis.Gestor);
        await cliente.PostAsync(Rotas.Projetos, new { nome = "Antes do rebaixamento" }).DeveRetornarAsync<Projeto>(HttpStatusCode.Created);

        await Gestor.PutAsync(Rotas.Responsavel(perfil.Id), new { nome = perfil.Nome, email = perfil.Email, papel = Papeis.Usuario, versao = perfil.Versao })
            .DeveRetornarAsync<Responsavel>(HttpStatusCode.OK);

        await cliente.PostAsync(Rotas.Projetos, new { nome = "Depois do rebaixamento" })
            .DeveSerProblemaAsync(HttpStatusCode.Forbidden, Codigos.AcessoNegado);
    }

    private async Task<string> ForjarTokenDoGestorAsync(DateTime expira, string? chave = null, string? emissor = null, string? audiencia = null)
    {
        var gestor = await PerfilDoGestorInicialAsync();
        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave ?? ApiFixture.JwtChaveSecreta)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: emissor ?? ApiFixture.JwtEmissor,
            audience: audiencia ?? ApiFixture.JwtAudiencia,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, gestor.UsuarioId),
                new Claim(ClaimTypes.NameIdentifier, gestor.UsuarioId),
                new Claim(JwtRegisteredClaimNames.Email, gestor.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, Papeis.Gestor),
            ],
            notBefore: expira.AddMinutes(-30),
            expires: expira,
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>Troca o payload por outro (e-mail diferente) mantendo a assinatura original.</summary>
    private static string Adulterar(string token)
    {
        var partes = token.Split('.');
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(partes[1]));
        var adulterado = payload.Replace(ApiFixture.GestorEmail, "atacante@contrato.test", StringComparison.Ordinal);
        Assert.NotEqual(payload, adulterado);
        return $"{partes[0]}.{Base64UrlEncoder.Encode(adulterado)}.{partes[2]}";
    }
}
