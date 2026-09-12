using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.DTOs.Paginacao;

namespace TaskManager.CleanArch.Application.Features.Tarefas;

public sealed record ListarTarefasQuery(Guid? ProjetoId, int Pagina = 1, int Tamanho = 20) : IRequest<ResultadoPaginado<TarefaDto>>;

public sealed class ListarTarefasQueryHandler(ITarefaRepository tarefas, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ListarTarefasQuery, ResultadoPaginado<TarefaDto>>
{
    public async Task<ResultadoPaginado<TarefaDto>> Handle(ListarTarefasQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();
        var pagina = new RequisicaoPaginada(request.Pagina, request.Tamanho);
        var resultado = await tarefas.ListarAsync(request.ProjetoId, pagina, cancellationToken);
        return ResultadoPaginado<TarefaDto>.Criar(resultado.Itens.Select(tarefa => tarefa.ParaDto()).ToArray(), pagina, resultado.Total);
    }
}
