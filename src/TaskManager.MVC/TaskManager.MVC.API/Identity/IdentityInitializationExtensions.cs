using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.API.Identity;

public static class IdentityInitializationExtensions
{
    public static async Task InitializeIdentityAsync(
        this IServiceProvider services,
        IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = provider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var context = provider.GetRequiredService<TaskManagerMvcDbContext>();

        await EnsureRolesAsync(roles);

        var initialManager = configuration
            .GetSection(InitialManagerOptions.SectionName)
            .Get<InitialManagerOptions>();
        if (initialManager is null ||
            string.IsNullOrWhiteSpace(initialManager.Nome) &&
            string.IsNullOrWhiteSpace(initialManager.Email) &&
            string.IsNullOrWhiteSpace(initialManager.Senha))
        {
            return;
        }

        Validate(initialManager);
        await EnsureInitialManagerAsync(initialManager, users, context);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var role in new[] { "Usuario", "Gestor" })
        {
            if (await roles.RoleExistsAsync(role))
            {
                continue;
            }

            EnsureSucceeded(await roles.CreateAsync(new IdentityRole(role)));
        }
    }

    private static async Task EnsureInitialManagerAsync(
        InitialManagerOptions options,
        UserManager<UsuarioIdentity> users,
        TaskManagerMvcDbContext context)
    {
        var email = options.Email!.Trim();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new UsuarioIdentity
            {
                UserName = email,
                Email = email,
                Ativo = true
            };
            EnsureSucceeded(await users.CreateAsync(user, options.Senha!));
        }

        if (!await users.IsInRoleAsync(user, "Gestor"))
        {
            EnsureSucceeded(await users.AddToRoleAsync(user, "Gestor"));
        }

        if (!await context.Responsaveis.AnyAsync(item => item.UsuarioId == user.Id))
        {
            context.Responsaveis.Add(new Responsavel
            {
                UsuarioId = user.Id,
                Nome = options.Nome!.Trim()
            });
            await context.SaveChangesAsync();
        }
    }

    private static void Validate(InitialManagerOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Nome) ||
            string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.Senha))
        {
            throw new InvalidOperationException(
                "GestorInicial:Nome, GestorInicial:Email e GestorInicial:Senha devem ser informados juntos.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }
}
