using TaskManager.CleanArch.Application.DTOs.Autenticacao;

namespace TaskManager.CleanArch.Application.Abstractions.Services;

public interface ITokenService
{
    TokenAcesso Gerar(DadosUsuarioToken usuario);
}
