using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record RemoverResponsavelTarefaCommand(Guid TarefaId, int Versao) : IRequest<TarefaDto>;

public sealed class RemoverResponsavelTarefaCommandHandler(
    ITarefaRepository tarefas,
    IProjetoRepository projetos,
    IStatusRepository status,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<RemoverResponsavelTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(RemoverResponsavelTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        return await unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, token)
                ?? throw new RecursoNaoEncontradoException("Tarefa");
            if (!await projetos.BloquearParaAtualizacaoAsync(tarefa.ProjetoId, token))
                throw new RecursoNaoEncontradoException("Projeto");

            RequestValidator.ExigirVersaoAtual(tarefa.Versao, request.Versao);
            var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, token)
                ?? throw new RecursoNaoEncontradoException("Status da tarefa");
            tarefa.RemoverResponsavel(statusAtual.Categoria);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return tarefa.ParaDto();
        }, cancellationToken);
    }
}
