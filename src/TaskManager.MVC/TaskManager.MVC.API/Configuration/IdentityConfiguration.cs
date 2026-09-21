using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Identity;

namespace TaskManager.MVC.API.Configuration;

public static class IdentityConfiguration
{
    public static IServiceCollection AddTaskManagerIdentity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("A configuração Jwt é obrigatória.");
        if (string.IsNullOrWhiteSpace(jwt.Emissor)
            || string.IsNullOrWhiteSpace(jwt.Audiencia)
            || Encoding.UTF8.GetByteCount(jwt.ChaveSecreta) < 32)
        {
            throw new InvalidOperationException("Jwt exige Emissor, Audiencia e ChaveSecreta com ao menos 32 bytes.");
        }

        services.Configure<JwtOptions>(configuration.GetRequiredSection(JwtOptions.SectionName));
        services.Configure<InitialManagerOptions>(configuration.GetSection(InitialManagerOptions.SectionName));
        services.AddIdentityCore<UsuarioIdentity>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<TaskManagerMvcDbContext>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.ChaveSecreta)),
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audiencia,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = CreateEvents();
            });
        services.AddAuthorization();
        services.AddScoped<TokenService>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<AccountService>();
        services.AddHostedService<IdentityBootstrapper>();
        return services;
    }

    private static JwtBearerEvents CreateEvents() => new()
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<UsuarioIdentity>>();
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            if (user is null || !user.Ativo)
            {
                context.Fail("Conta inativa ou inexistente.");
                return;
            }

            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                foreach (var claim in identity.FindAll(ClaimTypes.Role).ToArray())
                {
                    identity.RemoveClaim(claim);
                }

                foreach (var role in await userManager.GetRolesAsync(user))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        },
        OnChallenge = context =>
        {
            context.HandleResponse();
            return WriteProblemAsync(context.HttpContext, 401, "NAO_AUTENTICADO", "Não autenticado.");
        },
        OnForbidden = context => WriteProblemAsync(context.HttpContext, 403, "ACESSO_NEGADO", "Acesso negado.")
    };

    private static async Task WriteProblemAsync(HttpContext context, int status, string code, string title)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail = title,
            instance = context.Request.Path.Value,
            code
        });
    }
}
