using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record AtribuirResponsavelTarefaCommand(Guid TarefaId, Guid ResponsavelId, int Versao) : IRequest<TarefaDto>;

public sealed class AtribuirResponsavelTarefaCommandHandler(
    ITarefaRepository tarefas,
    IProjetoRepository projetos,
    IResponsavelRepository responsaveis,
    IStatusRepository status,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<AtribuirResponsavelTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(AtribuirResponsavelTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        RequestValidator.ExigirIdInformado(request.ResponsavelId, "responsavelId");

        return await unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            var tarefa = await tarefas.ObterPorIdAsync(request.TarefaId, token)
                ?? throw new RecursoNaoEncontradoException("Tarefa");
            if (!await projetos.BloquearParaAtualizacaoAsync(tarefa.ProjetoId, token))
                throw new RecursoNaoEncontradoException("Projeto");

            RequestValidator.ExigirVersaoAtual(tarefa.Versao, request.Versao);
            var statusAtual = await status.ObterPorIdAsync(tarefa.StatusId, token)
                ?? throw new RecursoNaoEncontradoException("Status da tarefa");
            if (!await responsaveis.BloquearParaAtualizacaoAsync(request.ResponsavelId, token))
                throw new ArgumentException("O responsável informado não existe.", nameof(request.ResponsavelId));

            var responsavel = await responsaveis.ObterPorIdAsync(request.ResponsavelId, token)
                ?? throw new ArgumentException("O responsável informado não existe.", nameof(request.ResponsavelId));

            tarefa.AtribuirResponsavel(responsavel.Id, statusAtual.Categoria, responsavel.Ativo);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return tarefa.ParaDto();
        }, cancellationToken);
    }
}
