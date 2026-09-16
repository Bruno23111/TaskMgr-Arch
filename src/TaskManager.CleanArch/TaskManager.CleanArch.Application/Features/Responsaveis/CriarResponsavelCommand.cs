using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record CriarResponsavelCommand(
    string Nome,
    string Email,
    string Senha,
    string Papel) : IRequest<ResponsavelContaDto>;

public sealed class CriarResponsavelCommandHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<CriarResponsavelCommand, ResponsavelContaDto>
{
    public Task<ResponsavelContaDto> Handle(CriarResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        PapeisSistema.Validar(request.Papel);
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new ArgumentException("O nome é obrigatório.", nameof(request.Nome));
        }

        return unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            var conta = await contas.CriarContaAsync(
                new CriarContaIdentity(request.Email, request.Senha, request.Papel), token);
            var responsavel = Responsavel.Criar(conta.UsuarioId, request.Nome.Trim());

            responsaveis.Adicionar(responsavel);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return responsavel.ParaDto(conta);
        }, cancellationToken);
    }
}
