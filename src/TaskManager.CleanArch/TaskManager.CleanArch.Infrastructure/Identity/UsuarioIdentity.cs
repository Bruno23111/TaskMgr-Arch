using Microsoft.AspNetCore.Identity;

namespace TaskManager.CleanArch.Infrastructure.Identity;

// Representa exclusivamente a conta de autenticação.
// O perfil de negócio continua sendo a entidade Responsavel, ligada pelo UsuarioId.
public sealed class UsuarioIdentity : IdentityUser
{
    public bool Ativo { get; set; } = true;
}
