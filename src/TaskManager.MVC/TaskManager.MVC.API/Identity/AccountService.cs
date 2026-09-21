using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Responsibles;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Models;
using System.Data;

namespace TaskManager.MVC.API.Identity;

public sealed class AccountService(
    TaskManagerMvcDbContext dbContext,
    UserManager<UsuarioIdentity> userManager)
{
    public async Task<List<ResponsibleResponse>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var accounts = await (
            from responsible in dbContext.Responsaveis.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on responsible.UsuarioId equals user.Id
            orderby responsible.Nome, responsible.Id
            select new { Responsible = responsible, User = user }).ToListAsync(cancellationToken);

        var responses = new List<ResponsibleResponse>(accounts.Count);
        foreach (var account in accounts)
        {
            responses.Add(await ToResponseAsync(account.Responsible, account.User));
        }
        return responses;
    }

    public async Task<ResponsibleResponse?> ObterAsync(Guid responsavelId, CancellationToken cancellationToken = default)
    {
        var responsible = await dbContext.Responsaveis.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == responsavelId, cancellationToken);
        if (responsible is null) return null;
        var user = await userManager.FindByIdAsync(responsible.UsuarioId);
        return user is null ? null : await ToResponseAsync(responsible, user);
    }

    public async Task<Responsavel> CriarAsync(
        string nome,
        string email,
        string senha,
        string papel,
        CancellationToken cancellationToken = default)
    {
        EnsureRole(papel);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var usuario = new UsuarioIdentity { UserName = email.Trim(), Email = email.Trim(), Ativo = true };
        EnsureIdentityResult(await userManager.CreateAsync(usuario, senha));
        EnsureIdentityResult(await userManager.AddToRoleAsync(usuario, papel));

        var responsavel = new Responsavel { UsuarioId = usuario.Id, Nome = nome.Trim() };
        dbContext.Responsaveis.Add(responsavel);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return responsavel;
    }

    public async Task<ResponsibleResponse> AtualizarAsync(
        Guid responsavelId,
        ResponsibleUpdateRequest request,
        string usuarioAtualId,
        CancellationToken cancellationToken = default)
    {
        EnsureRole(request.Papel);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var responsible = await dbContext.ResponsavelAsync(responsavelId, cancellationToken) ?? throw NotFound();
        EnsureVersion(responsible, request.Versao);
        var user = await userManager.FindByIdAsync(responsible.UsuarioId) ?? throw NotFound();
        var currentRoles = await userManager.GetRolesAsync(user);

        if (request.Papel == "Usuario" && currentRoles.Contains("Gestor"))
        {
            await LockManagerMembershipAsync(cancellationToken);
            await EnsureCanDemoteManagerAsync(user);
        }

        var normalizedName = request.Nome.Trim();
        if (normalizedName.Length == 0) throw InvalidData("O nome é obrigatório.");
        var normalizedEmail = request.Email.Trim();
        var changed = responsible.Nome != normalizedName
            || !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)
            || !currentRoles.Contains(request.Papel);

        if (changed && !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            EnsureIdentityResult(await userManager.SetEmailAsync(user, normalizedEmail));
            EnsureIdentityResult(await userManager.SetUserNameAsync(user, normalizedEmail));
        }
        if (changed && !currentRoles.Contains(request.Papel))
        {
            EnsureIdentityResult(await userManager.RemoveFromRolesAsync(user, currentRoles));
            EnsureIdentityResult(await userManager.AddToRoleAsync(user, request.Papel));
        }

        responsible.Nome = normalizedName;
        dbContext.Entry(responsible).State = EntityState.Modified;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToResponseAsync(responsible, user);
    }

    public async Task<ResponsibleResponse> DesativarAsync(
        Guid responsavelId,
        int versao,
        string usuarioAtualId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var responsavel = await dbContext.ResponsavelAsync(responsavelId, cancellationToken)
            ?? throw NotFound();
        EnsureVersion(responsavel, versao);
        var usuario = await userManager.FindByIdAsync(responsavel.UsuarioId) ?? throw NotFound();
        await LockManagerMembershipAsync(cancellationToken);
        await EnsureCanProtectManagerAsync(usuario, usuarioAtualId);

        if (!responsavel.Ativo && !usuario.Ativo)
        {
            await transaction.CommitAsync(cancellationToken);
            return await ToResponseAsync(responsavel, usuario);
        }

        var hasOpenTask = await (
            from tarefa in dbContext.Tarefas
            join status in dbContext.Statuses on tarefa.StatusId equals status.Id
            where tarefa.ResponsavelId == responsavelId && status.Categoria != CategoriaStatus.Concluida
            select tarefa.Id).AnyAsync(cancellationToken);
        if (hasOpenTask)
        {
            throw new ApiException(409, "REGISTRO_EM_USO", "Registro em uso.", "O responsável possui tarefa aberta.");
        }

        usuario.Ativo = false;
        responsavel.Ativo = false;
        EnsureIdentityResult(await userManager.UpdateAsync(usuario));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await ToResponseAsync(responsavel, usuario);
    }

    public async Task AlterarPapelAsync(
        Guid responsavelId,
        string novoPapel,
        string usuarioAtualId,
        CancellationToken cancellationToken = default)
    {
        EnsureRole(novoPapel);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var responsavel = await dbContext.ResponsavelAsync(responsavelId, cancellationToken)
            ?? throw NotFound();
        var usuario = await userManager.FindByIdAsync(responsavel.UsuarioId) ?? throw NotFound();

        if (novoPapel == "Usuario")
        {
            await EnsureCanProtectManagerAsync(usuario, usuarioAtualId);
        }

        var currentRoles = await userManager.GetRolesAsync(usuario);
        EnsureIdentityResult(await userManager.RemoveFromRolesAsync(usuario, currentRoles));
        EnsureIdentityResult(await userManager.AddToRoleAsync(usuario, novoPapel));
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ExcluirAsync(
        Guid responsavelId,
        int versao,
        string usuarioAtualId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var responsavel = await dbContext.ResponsavelAsync(responsavelId, cancellationToken)
            ?? throw NotFound();
        EnsureVersion(responsavel, versao);
        var usuario = await userManager.FindByIdAsync(responsavel.UsuarioId) ?? throw NotFound();
        await LockManagerMembershipAsync(cancellationToken);
        await EnsureCanProtectManagerAsync(usuario, usuarioAtualId);

        var referenced = await dbContext.Tarefas
            .IgnoreQueryFilters()
            .AnyAsync(tarefa => tarefa.ResponsavelId == responsavelId, cancellationToken);
        if (referenced)
        {
            throw new ApiException(409, "REGISTRO_EM_USO", "Registro em uso.", "O responsável está vinculado a uma ou mais tarefas.");
        }

        dbContext.Responsaveis.Remove(responsavel);
        await dbContext.SaveChangesAsync(cancellationToken);
        EnsureIdentityResult(await userManager.DeleteAsync(usuario));
        await transaction.CommitAsync(cancellationToken);
    }

    private Task LockManagerMembershipAsync(CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(
            "LOCK TABLE \"AspNetUserRoles\" IN SHARE ROW EXCLUSIVE MODE",
            cancellationToken);

    private static void EnsureVersion(Responsavel responsible, int version)
    {
        if (version != responsible.Versao)
        {
            throw new ApiException(409, "CONCORRENCIA", "Conflito de concorrência.", "O responsável foi alterado por outra operação.");
        }
    }

    private async Task<ResponsibleResponse> ToResponseAsync(Responsavel responsible, UsuarioIdentity user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new ResponsibleResponse(
            responsible.Id,
            responsible.UsuarioId,
            responsible.Nome,
            user.Email ?? string.Empty,
            responsible.Ativo && user.Ativo,
            roles.SingleOrDefault() ?? "Usuario",
            responsible.Versao);
    }

    private async Task EnsureCanProtectManagerAsync(UsuarioIdentity usuario, string usuarioAtualId)
    {
        if (usuario.Id == usuarioAtualId)
        {
            throw ProtectedManager("O Gestor não pode desativar, excluir ou rebaixar a própria conta.");
        }

        if (!await userManager.IsInRoleAsync(usuario, "Gestor"))
        {
            return;
        }

        var managers = await userManager.GetUsersInRoleAsync("Gestor");
        if (managers.Count(item => item.Ativo) <= 1)
        {
            throw ProtectedManager("O último Gestor ativo não pode ser desativado, excluído ou rebaixado.");
        }
    }

    private async Task EnsureCanDemoteManagerAsync(UsuarioIdentity usuario)
    {
        if (!await userManager.IsInRoleAsync(usuario, "Gestor"))
            return;

        var managers = await userManager.GetUsersInRoleAsync("Gestor");
        if (managers.Count(item => item.Ativo) <= 1)
            throw ProtectedManager("O Ãºltimo Gestor ativo nÃ£o pode ser rebaixado.");
    }

    private static void EnsureRole(string role)
    {
        if (role is not ("Usuario" or "Gestor"))
        {
            throw new ApiException(400, "DADOS_INVALIDOS", "Dados inválidos.", "O papel deve ser Usuario ou Gestor.");
        }
    }

    private static void EnsureIdentityResult(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            var duplicate = result.Errors.Any(error => error.Code.Contains("Duplicate", StringComparison.Ordinal));
            throw new ApiException(
                duplicate ? 409 : 400,
                duplicate ? "VALOR_DUPLICADO" : "DADOS_INVALIDOS",
                duplicate ? "Valor duplicado." : "Dados inválidos.",
                string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }

    private static ApiException NotFound() =>
        new(404, "RECURSO_NAO_ENCONTRADO", "Recurso não encontrado.", "O responsável informado não existe.");

    private static ApiException ProtectedManager(string detail) =>
        new(409, "REGISTRO_PROTEGIDO", "Registro protegido.", detail);

    private static ApiException InvalidData(string detail) =>
        new(400, "DADOS_INVALIDOS", "Dados inválidos.", detail);
}
