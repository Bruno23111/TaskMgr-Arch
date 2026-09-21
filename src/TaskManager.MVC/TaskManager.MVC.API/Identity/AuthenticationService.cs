using Microsoft.AspNetCore.Identity;
using TaskManager.MVC.API.Configuration;
using TaskManager.MVC.API.Contracts.Auth;

namespace TaskManager.MVC.API.Identity;

public sealed class AuthenticationService(
    UserManager<UsuarioIdentity> userManager,
    TokenService tokenService)
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var usuario = await userManager.FindByEmailAsync(request.Email.Trim());
        cancellationToken.ThrowIfCancellationRequested();

        if (usuario is null || !usuario.Ativo || !await userManager.CheckPasswordAsync(usuario, request.Senha))
        {
            throw new ApiException(401, "NAO_AUTENTICADO", "Não autenticado.", "E-mail ou senha inválidos.");
        }

        return await tokenService.EmitirAsync(usuario, cancellationToken);
    }
}
