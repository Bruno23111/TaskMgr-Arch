using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Features.Responsaveis;

public sealed record ExcluirResponsavelCommand(Guid Id, int Versao) : IRequest<bool?>;

public sealed class ExcluirResponsavelCommandHandler(
    IContaIdentityService contas,
    IResponsavelRepository responsaveis,
    ITarefaRepository tarefas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<ExcluirResponsavelCommand, bool?>
{
    public Task<bool?> Handle(ExcluirResponsavelCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);

        return unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync<bool?>(async token =>
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

            RequestValidator.ExigirVersaoAtual(responsavel.Versao, request.Versao);
            if (responsavel.UsuarioId == usuarioAtual.UsuarioId)
            {
                throw new DomainRuleViolationException(
                    "REGISTRO_PROTEGIDO",
                    "Não é permitido excluir a própria conta.");
            }

            var conta = await contas.ObterContaAsync(responsavel.UsuarioId, token)
                ?? throw new InvalidOperationException("Conta de autenticação não encontrada.");
            if (conta.Ativo && conta.Papeis.Contains(PapeisSistema.Gestor) &&
                await contas.ContarGestoresAtivosAsync(token) <= 1)
            {
                throw new DomainRuleViolationException(
                    "REGISTRO_PROTEGIDO",
                    "O último gestor ativo não pode ser excluído.");
            }

            responsavel.GarantirPodeSerExcluido(await tarefas.PossuiTarefasParaResponsavelAsync(responsavel.Id, token));
            responsaveis.Remover(responsavel);
            await contas.ExcluirContaAsync(responsavel.UsuarioId, token);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);

            return true;
        }, cancellationToken);
    }
}
