using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.Application.Features.Status;

public sealed record CriarStatusCommand(string Codigo, string Nome, CategoriaStatus? Categoria) : IRequest<StatusTarefaDto>;

public sealed class CriarStatusCommandHandler(
    IStatusRepository status,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    IUsuarioAtual usuarioAtual) : IRequestHandler<CriarStatusCommand, StatusTarefaDto>
{
    public async Task<StatusTarefaDto> Handle(CriarStatusCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        if (request.Categoria is null)
            throw new ArgumentException("A categoria do status é obrigatória.", nameof(request.Categoria));

        var statusTarefa = StatusTarefa.Criar(request.Codigo, request.Nome, request.Categoria.Value);
        if (await status.CodigoEmUsoAsync(statusTarefa.Codigo, cancellationToken: cancellationToken))
            throw new ValorDuplicadoException("O código do status já está em uso.");

        status.Adicionar(statusTarefa);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return statusTarefa.ParaDto();
    }
}
