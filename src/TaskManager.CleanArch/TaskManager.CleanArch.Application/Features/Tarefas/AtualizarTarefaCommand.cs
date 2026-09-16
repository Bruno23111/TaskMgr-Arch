using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record AtualizarTarefaCommand(Guid TarefaId, string Titulo, string? Descricao, int Versao) : IRequest<TarefaDto>;

public sealed class AtualizarTarefaCommandHandler(
    ITarefaRepository tarefas,
    IStatusRepository status,
    IResponsavelRepository responsaveis,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<AtualizarTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(AtualizarTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        request.Versao.ExigirVersaoInformada();

        var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Tarefa");

        var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Status da tarefa");

        if (!usuarioAtual.Gestor)
        {
            var responsavelAtual = await responsaveis.ObterPorUsuarioIdAsync(usuarioAtual.UsuarioId, cancellationToken);
            if (responsavelAtual?.Id != tarefa.ResponsavelId)
                throw new AcessoNegadoException();
        }

        tarefa.Versao.ExigirVersao(request.Versao);
        tarefa.AtualizarConteudo(request.Titulo, request.Descricao, statusAtual.Categoria);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return tarefa.ParaDto();
    }
}
