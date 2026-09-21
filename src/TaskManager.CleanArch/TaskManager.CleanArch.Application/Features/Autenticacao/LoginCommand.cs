using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Features.Autenticacao;

public sealed record LoginCommand(string Email, string Senha) : IRequest<TokenAcesso>;

public sealed class LoginCommandHandler(
    IContaIdentityService contas,
    ITokenService tokens) : IRequestHandler<LoginCommand, TokenAcesso>
{
    public async Task<TokenAcesso> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = RequestValidator.ExigirEmail(request.Email);
        var senha = RequestValidator.ExigirValor(request.Senha, "senha");
        var conta = await contas.AutenticarAsync(email, senha, cancellationToken)
            ?? throw new CredenciaisInvalidasException();

        return tokens.Gerar(new DadosUsuarioToken(conta.UsuarioId, conta.Email, conta.Papeis));
    }
}
