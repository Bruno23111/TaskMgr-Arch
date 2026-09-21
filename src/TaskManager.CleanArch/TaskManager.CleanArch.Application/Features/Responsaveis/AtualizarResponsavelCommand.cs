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
        RequestValidator.ExigirVersaoInformada(request.Versao);
        PapeisSistema.Validar(request.Papel);
        var nome = RequestValidator.ExigirTexto(request.Nome, "nome", 100);
        var email = RequestValidator.ExigirEmail(request.Email);

        return unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            var responsavel = await responsaveis.ObterPorIdAsync(request.Id, token);
            if (responsavel is null)
            {
                return null;
            }

            RequestValidator.ExigirVersaoAtual(responsavel.Versao, request.Versao);
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

            responsavel.AtualizarNome(nome);
            await contas.AtualizarContaAsync(responsavel.UsuarioId, email, request.Papel, token);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return responsavel.ParaDto(conta with { Email = email, Papeis = [request.Papel] });
        }, cancellationToken);
    }
}
