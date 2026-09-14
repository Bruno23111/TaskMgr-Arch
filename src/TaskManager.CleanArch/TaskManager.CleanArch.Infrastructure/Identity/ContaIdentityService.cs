using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Infrastructure.Persistence;

namespace TaskManager.CleanArch.Infrastructure.Identity;

public sealed class ContaIdentityService(
    UserManager<UsuarioIdentity> userManager,
    TaskManagerCleanArchDbContext context) : IContaIdentityService
{
    public async Task<ContaAutenticada?> AutenticarAsync(string email, string senha, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
            return null;

        var usuario = await userManager.FindByEmailAsync(email.Trim());
        if (usuario is null || !usuario.Ativo || !await userManager.CheckPasswordAsync(usuario, senha))
            return null;

        return await CriarContaAutenticadaAsync(usuario);
    }

    public async Task<ContaAutenticada?> ObterAtivaAsync(string usuarioId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuarioId))
            return null;

        var usuario = await userManager.FindByIdAsync(usuarioId);
        return usuario is null || !usuario.Ativo
            ? null
            : await CriarContaAutenticadaAsync(usuario);
    }

    public async Task<IReadOnlyList<ResponsavelContaDto>> ListarResponsaveisComPerfisAsync(
        CancellationToken cancellationToken = default)
    {
        var perfis = await CriarConsultaResponsaveisComPerfis()
            .ToArrayAsync(cancellationToken);

        return perfis;
    }

    public Task<ResponsavelContaDto?> ObterResponsavelComPerfilAsync(
        Guid responsavelId,
        CancellationToken cancellationToken = default) =>
        CriarConsultaResponsaveisComPerfis(responsavelId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ResponsavelContaDto?> AtualizarResponsavelAsync(
        Guid responsavelId,
        AtualizarResponsavelConta dados,
        CancellationToken cancellationToken = default)
    {
        ValidarDadosAtualizacao(dados);

        await using var transacao = await context.Database.BeginTransactionAsync(cancellationToken);
        var responsavel = await context.Responsaveis
            .SingleOrDefaultAsync(item => item.Id == responsavelId, cancellationToken);

        if (responsavel is null)
            return null;

        var usuario = await userManager.FindByIdAsync(responsavel.UsuarioId);
        if (usuario is null)
            throw new InvalidOperationException("A conta do responsavel nao foi encontrada.");

        responsavel.AtualizarNome(dados.Nome);
        usuario.Email = dados.Email.Trim();
        usuario.UserName = dados.Email.Trim();

        var resultado = await userManager.UpdateAsync(usuario);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));

        var papeisAtuais = await userManager.GetRolesAsync(usuario);
        if (papeisAtuais.Count > 0)
        {
            resultado = await userManager.RemoveFromRolesAsync(usuario, papeisAtuais);
            if (!resultado.Succeeded)
                throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));
        }

        resultado = await userManager.AddToRoleAsync(usuario, dados.Papel);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));

        await context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return await CriarResponsavelContaDtoAsync(responsavel, usuario, dados.Papel);
    }

    public async Task<ResponsavelContaDto> CriarResponsavelAsync(
        CriarResponsavelConta dados,
        CancellationToken cancellationToken = default)
    {
        ValidarDadosCadastro(dados);

        await using var transacao = await context.Database.BeginTransactionAsync(cancellationToken);
        var usuario = new UsuarioIdentity
        {
            UserName = dados.Email.Trim(),
            Email = dados.Email.Trim(),
            Ativo = true
        };

        var resultado = await userManager.CreateAsync(usuario, dados.Senha);
        if (!resultado.Succeeded)
            throw new InvalidOperationException(string.Join(" ", resultado.Errors.Select(erro => erro.Description)));

        var papelAdicionado = await userManager.AddToRoleAsync(usuario, dados.Papel);
        if (!papelAdicionado.Succeeded)
            throw new InvalidOperationException(string.Join(" ", papelAdicionado.Errors.Select(erro => erro.Description)));

        var responsavel = Responsavel.Criar(usuario.Id, dados.Nome);
        context.Responsaveis.Add(responsavel);
        await context.SaveChangesAsync(cancellationToken);
        await transacao.CommitAsync(cancellationToken);

        return await CriarResponsavelContaDtoAsync(responsavel, usuario, dados.Papel);
    }

    private async Task<ContaAutenticada> CriarContaAutenticadaAsync(UsuarioIdentity usuario)
    {
        var papeis = await userManager.GetRolesAsync(usuario);
        return new ContaAutenticada(usuario.Id, usuario.Email!, papeis.ToArray());
    }

    private async Task<ResponsavelContaDto> CriarResponsavelContaDtoAsync(
        Responsavel responsavel,
        UsuarioIdentity usuario,
        string? papel = null)
    {
        var papelDaConta = papel ?? (await userManager.GetRolesAsync(usuario)).SingleOrDefault() ?? string.Empty;

        return new ResponsavelContaDto(
            responsavel.Id,
            usuario.Id,
            responsavel.Nome,
            usuario.Email ?? string.Empty,
            papelDaConta,
            responsavel.Ativo && usuario.Ativo);
    }

    private IQueryable<ResponsavelContaDto> CriarConsultaResponsaveisComPerfis(Guid? responsavelId = null) =>
        from responsavel in context.Responsaveis.AsNoTracking()
        join usuario in context.Users.AsNoTracking() on responsavel.UsuarioId equals usuario.Id
        join usuarioPapel in context.UserRoles.AsNoTracking() on usuario.Id equals usuarioPapel.UserId into usuarioPapeis
        from usuarioPapel in usuarioPapeis.DefaultIfEmpty()
        join papel in context.Roles.AsNoTracking() on usuarioPapel.RoleId equals papel.Id into papeis
        from papel in papeis.DefaultIfEmpty()
        where !responsavelId.HasValue || responsavel.Id == responsavelId.Value
        orderby responsavel.Nome
        select new ResponsavelContaDto(
            responsavel.Id,
            usuario.Id,
            responsavel.Nome,
            usuario.Email ?? string.Empty,
            papel.Name ?? string.Empty,
            responsavel.Ativo && usuario.Ativo);

    private static void ValidarDadosCadastro(CriarResponsavelConta dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(dados.Email) || string.IsNullOrWhiteSpace(dados.Senha))
            throw new ArgumentException("Nome, e-mail e senha são obrigatórios.", nameof(dados));

        if (!PapeisSistema.Valido(dados.Papel))
            throw new ArgumentException("O papel deve ser Usuario ou Gestor.", nameof(dados));
    }

    private static void ValidarDadosAtualizacao(AtualizarResponsavelConta dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        if (string.IsNullOrWhiteSpace(dados.Nome) || string.IsNullOrWhiteSpace(dados.Email) || string.IsNullOrWhiteSpace(dados.Papel))
            throw new ArgumentException("Nome, e-mail e papel sao obrigatorios.", nameof(dados));

        if (!PapeisSistema.Valido(dados.Papel))
            throw new ArgumentException("O papel deve ser Usuario ou Gestor.", nameof(dados));
    }
}
