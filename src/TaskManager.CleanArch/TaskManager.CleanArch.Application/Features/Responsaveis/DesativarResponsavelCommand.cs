using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs.Autenticacao;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record DesativarResponsavelCommand(Guid Id, int Versao) : IRequest<ResponsavelContaDto?>;

public sealed class DesativarResponsavelCommandHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    ITarefaRepository tarefas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<DesativarResponsavelCommand, ResponsavelContaDto?>
{
    public Task<ResponsavelContaDto?> Handle(DesativarResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        request.Versao.ExigirVersaoInformada();

        return unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            if (!await responsaveis.BloquearParaAtualizacaoAsync(request.Id, token))
            {
                return null;
            }

            var responsavel = await responsaveis.ObterPorIdAsync(request.Id, token);
            if (responsavel is null)
            {
                return null;
            }

            responsavel.Versao.ExigirVersao(request.Versao);
            if (responsavel.UsuarioId == usuarioAtual.UsuarioId)
            {
                throw new DomainRuleViolationException(
                    "REGISTRO_PROTEGIDO",
                    "Não é permitido desativar a própria conta.");
            }

            var conta = await contas.ObterContaAsync(responsavel.UsuarioId, token)
                ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");
            if (conta.Ativo && conta.Papeis.Contains(PapeisSistema.Gestor) &&
                await contas.ContarGestoresAtivosAsync(token) <= 1)
            {
                throw new DomainRuleViolationException(
                    "REGISTRO_PROTEGIDO",
                    "O último gestor ativo não pode ser desativado.");
            }

            responsavel.Desativar(await tarefas.PossuiTarefasVisiveisNaoConcluidasParaResponsavelAsync(responsavel.Id, token));
            await contas.DesativarContaAsync(responsavel.UsuarioId, token);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return responsavel.ParaDto(conta with { Ativo = false });
        }, cancellationToken);
    }
}
