using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskManager.MVC.API.Contracts.Auth;

namespace TaskManager.MVC.API.Identity;

public sealed class TokenService(
    UserManager<UsuarioIdentity> userManager,
    IOptions<JwtOptions> options)
{
    public async Task<LoginResponse> EmitirAsync(
        UsuarioIdentity usuario,
        CancellationToken cancellationToken = default)
    {
        var jwt = options.Value;
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var roles = await userManager.GetRolesAsync(usuario);
        cancellationToken.ThrowIfCancellationRequested();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id),
            new(ClaimTypes.NameIdentifier, usuario.Id),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: jwt.Emissor,
            audience: jwt.Audiencia,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
