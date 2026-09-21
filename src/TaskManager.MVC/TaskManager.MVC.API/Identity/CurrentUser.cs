using System.Security.Claims;
using TaskManager.MVC.API.Configuration;

namespace TaskManager.MVC.API.Identity;

public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor)
{
    private ClaimsPrincipal Principal => httpContextAccessor.HttpContext?.User
        ?? throw new ApiException(401, "NAO_AUTENTICADO", "Não autenticado.", "Não foi possível identificar o usuário atual.");

    public string Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new ApiException(401, "NAO_AUTENTICADO", "Não autenticado.", "O token não contém a identificação do usuário.");

    public bool IsManager => Principal.IsInRole("Gestor");
}
