using System.Security.Claims;
using TaskManager.CleanArch.Application.Abstractions.Identity;

namespace TaskManager.CleanArch.WebAPI.Services;

public sealed class UsuarioAtualHttpContext(IHttpContextAccessor httpContextAccessor) : IUsuarioAtual
{
    private ClaimsPrincipal? Usuario => httpContextAccessor.HttpContext?.User;

    public string UsuarioId => Usuario?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public bool Autenticado => Usuario?.Identity?.IsAuthenticated == true;

    public bool Gestor => Usuario?.IsInRole("Gestor") == true;
}
