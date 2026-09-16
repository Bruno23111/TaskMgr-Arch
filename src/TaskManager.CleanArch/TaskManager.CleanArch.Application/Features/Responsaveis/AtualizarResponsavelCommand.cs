using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record AtualizarResponsavelCommand(
    Guid Id,
    string Nome,
    string Email,
    string Papel,
    int Versao) : IRequest<ResponsavelContaDto?>;

public sealed class AtualizarResponsavelCommandHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<AtualizarResponsavelCommand, ResponsavelContaDto?>
{
    public Task<ResponsavelContaDto?> Handle(AtualizarResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        request.Versao.ExigirVersaoInformada();
        PapeisSistema.Validar(request.Papel);
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new ArgumentException("O nome é obrigatório.", nameof(request.Nome));
        }

        return unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            var responsavel = await responsaveis.ObterPorIdAsync(request.Id, token);
            if (responsavel is null)
            {
                return null;
            }

            responsavel.Versao.ExigirVersao(request.Versao);
            var conta = await contas.ObterContaAsync(responsavel.UsuarioId, token)
                ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");

            var rebaixaUltimoGestor = conta.Ativo &&
                conta.Papeis.Contains(PapeisSistema.Gestor) &&
                request.Papel != PapeisSistema.Gestor;
            if (rebaixaUltimoGestor && await contas.ContarGestoresAtivosAsync(token) <= 1)
            {
                throw new DomainRuleViolationException(
                    "REGISTRO_PROTEGIDO",
                    "O último gestor ativo não pode ser rebaixado.");
            }

            responsavel.AtualizarNome(request.Nome.Trim());
            await contas.AtualizarContaAsync(responsavel.UsuarioId, request.Email, request.Papel, token);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return responsavel.ParaDto(conta with { Email = request.Email.Trim(), Papeis = [request.Papel] });
        }, cancellationToken);
    }
}
