using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TaskManager.MVC.API.Identity;

public sealed class IdentityBootstrapper(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { "Usuario", "Gestor" })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Não foi possível criar o papel {role}.");
                }
            }
        }

        var options = scope.ServiceProvider.GetRequiredService<IOptions<InitialManagerOptions>>().Value;
        var values = new[] { options.Nome, options.Email, options.Senha };
        if (values.All(string.IsNullOrWhiteSpace))
        {
            return;
        }

        if (values.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("GestorInicial exige Nome, Email e Senha quando configurado.");
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioIdentity>>();
        if (await userManager.FindByEmailAsync(options.Email!) is null)
        {
            var accountService = scope.ServiceProvider.GetRequiredService<AccountService>();
            await accountService.CriarAsync(options.Nome!, options.Email!, options.Senha!, "Gestor", cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
