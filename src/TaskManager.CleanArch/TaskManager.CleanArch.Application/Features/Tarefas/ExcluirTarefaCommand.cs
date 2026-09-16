using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record ExcluirTarefaCommand(Guid TarefaId, int Versao) : IRequest;

public sealed class ExcluirTarefaCommandHandler(
    ITarefaRepository tarefas,
    IProjetoRepository projetos,
    IStatusRepository status,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual,
    IRelogio relogio) : IRequestHandler<ExcluirTarefaCommand>
{
    public async Task Handle(ExcluirTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        request.Versao.ExigirVersaoInformada();
        await unidadeDeTrabalho.ExecutarEmTransacaoAsync(async token =>
        {
            var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, token)
                ?? throw new RecursoNaoEncontradoException("Tarefa");
            if (!await projetos.BloquearParaAtualizacaoAsync(tarefa.ProjetoId, token))
                throw new RecursoNaoEncontradoException("Projeto");

            tarefa.Versao.ExigirVersao(request.Versao);
            var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, token)
                ?? throw new RecursoNaoEncontradoException("Status da tarefa");
            tarefa.Excluir(statusAtual.Categoria, relogio.DataHoraAgoraUTC);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return true;
        }, cancellationToken);
    }
}
