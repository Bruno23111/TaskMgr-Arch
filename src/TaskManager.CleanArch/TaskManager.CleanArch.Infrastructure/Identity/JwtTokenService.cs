using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Infrastructure.Identity;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public TokenAcesso Gerar(DadosUsuarioToken usuario)
    {
        ValidarUsuario(usuario);

        var configuracao = options.Value;
        configuracao.Validar();

        var emitidoEm = DateTimeOffset.UtcNow;
        var expiraEm = emitidoEm.AddMinutes(configuracao.DuracaoEmMinutos);
        var credenciais = CriarCredenciais(configuracao.ChaveSecreta);

        var token = new JwtSecurityToken(
            issuer: configuracao.Emissor,
            audience: configuracao.Audiencia,
            claims: CriarClaims(usuario),
            notBefore: emitidoEm.UtcDateTime,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        var valor = new JwtSecurityTokenHandler().WriteToken(token);
        return new TokenAcesso(valor, expiraEm);
    }

    private static SigningCredentials CriarCredenciais(string chaveSecreta)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chaveSecreta));
        return new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
    }

    private static IReadOnlyCollection<Claim> CriarClaims(DadosUsuarioToken usuario)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.UsuarioId),
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId),
            new(JwtRegisteredClaimNames.Email, usuario.Email),
            new(ClaimTypes.Email, usuario.Email)
        };

        claims.AddRange(usuario.Papeis.Distinct(StringComparer.Ordinal)
            .Select(papel => new Claim(ClaimTypes.Role, papel)));

        return claims;
    }

    private static void ValidarUsuario(DadosUsuarioToken usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        if (string.IsNullOrWhiteSpace(usuario.UsuarioId))
            throw new ArgumentException("O identificador do usuário é obrigatório.", nameof(usuario));

        if (string.IsNullOrWhiteSpace(usuario.Email))
            throw new ArgumentException("O e-mail do usuário é obrigatório.", nameof(usuario));

        ArgumentNullException.ThrowIfNull(usuario.Papeis);
    }
}
