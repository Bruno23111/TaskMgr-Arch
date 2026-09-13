using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Infrastructure.Persistence;

namespace TaskManager.CleanArch.Infrastructure.Identity;

public static class IdentityInitializationExtensions
{
    public static async Task InicializarIdentityAsync(this IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var roles = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var usuarios = provider.GetRequiredService<UserManager<UsuarioIdentity>>();
        var context = provider.GetRequiredService<TaskManagerCleanArchDbContext>();

        await GarantirPapeisAsync(roles);

        var gestorInicial = new GestorInicialOptions();
        configuration.GetSection(GestorInicialOptions.NomeSecao).Bind(gestorInicial);
        if (!gestorInicial.Configurado)
            return;

        gestorInicial.Validar();
        await GarantirGestorInicialAsync(gestorInicial, usuarios, context);
    }

    private static async Task GarantirPapeisAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var papel in new[] { PapeisSistema.Usuario, PapeisSistema.Gestor })
        {
            if (await roles.RoleExistsAsync(papel))
                continue;

            var resultado = await roles.CreateAsync(new IdentityRole(papel));
            if (!resultado.Succeeded)
                throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
        }
    }

    private static async Task GarantirGestorInicialAsync(
        GestorInicialOptions gestorInicial,
        UserManager<UsuarioIdentity> usuarios,
        TaskManagerCleanArchDbContext context)
    {
        var usuario = await usuarios.FindByEmailAsync(gestorInicial.Email.Trim());
        if (usuario is null)
        {
            usuario = new UsuarioIdentity
            {
                UserName = gestorInicial.Email.Trim(),
                Email = gestorInicial.Email.Trim(),
                Ativo = true
            };

            var resultado = await usuarios.CreateAsync(usuario, gestorInicial.Senha);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
        }

        if (!await usuarios.IsInRoleAsync(usuario, PapeisSistema.Gestor))
        {
            var resultado = await usuarios.AddToRoleAsync(usuario, PapeisSistema.Gestor);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
        }

        var perfilExiste = await context.Responsaveis.AnyAsync(responsavel => responsavel.UsuarioId == usuario.Id);
        if (perfilExiste)
            return;

        context.Responsaveis.Add(Responsavel.Criar(usuario.Id, gestorInicial.Nome));
        await context.SaveChangesAsync();
    }
}
