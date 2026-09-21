using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record CriarTarefaCommand(Guid ProjetoId, string Titulo, string? Descricao) : IRequest<TarefaDto>;

public sealed class CriarTarefaCommandHandler(
    IProjetoRepository projetos,
    IStatusRepository status,
    ITarefaRepository tarefas,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<CriarTarefaCommand, TarefaDto>
{
    public async Task<TarefaDto> Handle(CriarTarefaCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirIdInformado(request.ProjetoId, "projetoId");
        var titulo = RequestValidator.ExigirTexto(request.Titulo, "titulo", 100);
        var descricao = RequestValidator.ValidarTextoOpcional(request.Descricao, "descricao", 255);
        return await unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            if (!await projetos.BloquearParaAtualizacaoAsync(request.ProjetoId, token))
                throw new ArgumentException("O projeto informado não existe.", nameof(request.ProjetoId));

            var statusInicial = await status.ObterPendenteInicialAsync(token)
                ?? throw new InvalidOperationException("O status inicial pendente não está configurado.");

            var tarefa = Tarefa.Criar(request.ProjetoId, titulo, descricao, statusInicial.Id);
            tarefas.Adicionar(tarefa);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return tarefa.ParaDto();
        }, cancellationToken);
    }
}
