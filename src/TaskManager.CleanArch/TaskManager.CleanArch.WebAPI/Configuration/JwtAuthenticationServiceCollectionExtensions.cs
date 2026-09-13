using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Infrastructure.Identity;

namespace TaskManager.CleanArch.WebAPI.Configuration;

public static class JwtAuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddTaskManagerJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = new JwtOptions();
        configuration.GetSection(JwtOptions.NomeSecao).Bind(jwt);
        jwt.Validar();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),
                    ValidateLifetime = true,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = AtualizarContaEPerfisAsync
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static async Task AtualizarContaEPerfisAsync(TokenValidatedContext context)
    {
        var usuarioId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var contas = context.HttpContext.RequestServices.GetRequiredService<IContaIdentityService>();
        var conta = await contas.ObterAtivaAsync(usuarioId ?? string.Empty, context.HttpContext.RequestAborted);

        if (conta is null)
        {
            context.Fail("A conta não está ativa.");
            return;
        }

        if (context.Principal?.Identity is not ClaimsIdentity identidade)
        {
            context.Fail("A identidade do token é inválida.");
            return;
        }

        foreach (var claim in identidade.FindAll(ClaimTypes.Role).ToArray())
            identidade.RemoveClaim(claim);

        foreach (var papel in conta.Papeis)
            identidade.AddClaim(new Claim(ClaimTypes.Role, papel));
    }
}
