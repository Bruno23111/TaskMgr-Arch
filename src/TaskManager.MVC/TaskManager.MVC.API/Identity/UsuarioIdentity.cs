using Microsoft.AspNetCore.Identity;

namespace TaskManager.MVC.API.Identity;

public sealed class UsuarioIdentity : IdentityUser
{
    public bool Ativo { get; set; } = true;
}
