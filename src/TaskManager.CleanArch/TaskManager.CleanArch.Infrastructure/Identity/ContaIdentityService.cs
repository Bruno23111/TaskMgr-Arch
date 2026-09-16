using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Infrastructure.Persistence;

namespace TaskManager.CleanArch.Infrastructure.Identity;

public sealed class ContaIdentityService(
    UserManager<UsuarioIdentity> userManager,
    TaskManagerCleanArchDbContext context) : IContaIdentityService
{
    public async Task<ContaAutenticada?> AutenticarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            return null;
        }

        var usuario = await userManager.FindByEmailAsync(email.Trim());
        if (usuario is null || !usuario.Ativo || !await userManager.CheckPasswordAsync(usuario, senha))
        {
            return null;
        }

        return await CriarContaAutenticadaAsync(usuario);
    }

    public async Task<ContaAutenticada?> ObterAtivaAsync(
        string usuarioId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
        {
            return null;
        }

        var usuario = await userManager.FindByIdAsync(usuarioId);
        return usuario is null || !usuario.Ativo ? null : await CriarContaAutenticadaAsync(usuario);
    }

    public async Task<ContaIdentityDto?> ObterContaAsync(
        string usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId);
        return usuario is null ? null : await CriarContaDtoAsync(usuario);
    }

    public async Task<IReadOnlyDictionary<string, ContaIdentityDto>> ObterContasAsync(
        IEnumerable<string> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        var ids = usuarioIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return new Dictionary<string, ContaIdentityDto>();
        }

        var usuarios = await context.Users
            .AsNoTracking()
            .Where(usuario => ids.Contains(usuario.Id))
            .ToArrayAsync(cancellationToken);

        var contas = new Dictionary<string, ContaIdentityDto>(usuarios.Length);
        foreach (var usuario in usuarios)
        {
            contas[usuario.Id] = await CriarContaDtoAsync(usuario);
        }

        return contas;
    }

    public async Task<ContaIdentityDto> CriarContaAsync(
        CriarContaIdentity dados,
        CancellationToken cancellationToken = default)
    {
        PapeisSistema.Validar(dados.Papel);
        if (string.IsNullOrWhiteSpace(dados.Email) || string.IsNullOrWhiteSpace(dados.Senha))
        {
            throw new ArgumentException("E-mail e senha são obrigatórios.", nameof(dados));
        }

        var email = dados.Email.Trim();

        var usuario = new UsuarioIdentity
        {
            UserName = email,
            Email = email,
            Ativo = true
        };

        LancarSeFalha(await userManager.CreateAsync(usuario, dados.Senha));
        LancarSeFalha(await userManager.AddToRoleAsync(usuario, dados.Papel));

        return new ContaIdentityDto(usuario.Id, usuario.Email!, usuario.Ativo, [dados.Papel]);
    }

    public async Task AtualizarContaAsync(
        string usuarioId,
        string email,
        string papel,
        CancellationToken cancellationToken = default)
    {
        PapeisSistema.Validar(papel);
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("O e-mail é obrigatório.", nameof(email));
        }

        var usuario = await userManager.FindByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");

        usuario.Email = email.Trim();
        usuario.UserName = email.Trim();
        LancarSeFalha(await userManager.UpdateAsync(usuario));

        var papeisAtuais = await userManager.GetRolesAsync(usuario);
        if (papeisAtuais.Count > 0)
        {
            LancarSeFalha(await userManager.RemoveFromRolesAsync(usuario, papeisAtuais));
        }

        LancarSeFalha(await userManager.AddToRoleAsync(usuario, papel));
    }

    public async Task DesativarContaAsync(
        string usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");

        usuario.Ativo = false;
        LancarSeFalha(await userManager.UpdateAsync(usuario));
    }

    public async Task ExcluirContaAsync(
        string usuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByIdAsync(usuarioId)
            ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");

        LancarSeFalha(await userManager.DeleteAsync(usuario));
    }

    public Task<int> ContarGestoresAtivosAsync(CancellationToken cancellationToken = default) =>
        (from usuario in context.Users.AsNoTracking()
         join usuarioPapel in context.UserRoles.AsNoTracking() on usuario.Id equals usuarioPapel.UserId
         join papel in context.Roles.AsNoTracking() on usuarioPapel.RoleId equals papel.Id
         where usuario.Ativo && papel.Name == PapeisSistema.Gestor
         select usuario.Id)
        .CountAsync(cancellationToken);

    private async Task<ContaAutenticada> CriarContaAutenticadaAsync(UsuarioIdentity usuario)
    {
        var papeis = await userManager.GetRolesAsync(usuario);
        return new ContaAutenticada(usuario.Id, usuario.Email!, papeis.ToArray());
    }

    private async Task<ContaIdentityDto> CriarContaDtoAsync(UsuarioIdentity usuario)
    {
        var papeis = await userManager.GetRolesAsync(usuario);
        return new ContaIdentityDto(usuario.Id, usuario.Email!, usuario.Ativo, papeis.ToArray());
    }

    private static void LancarSeFalha(IdentityResult resultado)
    {
        if (resultado.Succeeded)
        {
            return;
        }

        var erros = resultado.Errors.ToArray();
        if (erros.Any(erro => erro.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new ValorDuplicadoException("Já existe uma conta cadastrada com este e-mail.");
        }

        if (erros.Any(erro => erro.Code == "ConcurrencyFailure"))
        {
            throw new ConflitoDeConcorrenciaException();
        }

        throw new ArgumentException(string.Join(" ", erros.Select(erro => erro.Description)));
    }
}
