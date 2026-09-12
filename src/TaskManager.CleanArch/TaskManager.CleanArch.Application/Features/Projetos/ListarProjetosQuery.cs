using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Application.DTOs.Paginacao;

namespace TaskManager.CleanArch.Application.Features.Projetos;

public sealed record ListarProjetosQuery(int Pagina = 1, int Tamanho = 20) : IRequest<ResultadoPaginado<ProjetoDto>>;

public sealed class ListarProjetosQueryHandler(IProjetoRepository projetos, IUsuarioAtual usuarioAtual)
    : IRequestHandler<ListarProjetosQuery, ResultadoPaginado<ProjetoDto>>
{
    public async Task<ResultadoPaginado<ProjetoDto>> Handle(ListarProjetosQuery request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirAutenticado();

        var pagina = new RequisicaoPaginada(request.Pagina, request.Tamanho);

        var resultado = await projetos.ListarAsync(pagina, cancellationToken);

        return ResultadoPaginado<ProjetoDto>
            .Criar(resultado.Itens
            .Select(projeto => projeto
                .ParaDto())
                .ToArray(),
                pagina,
                resultado.Total);
    }
}
