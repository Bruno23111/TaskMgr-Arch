using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;
using TaskManager.CleanArch.Domain.Entities;

namespace TaskManager.CleanArch.Application.Features.Projetos;

public sealed record CriarProjetoCommand(string Nome, string? Descricao) : IRequest<ProjetoDto>;

public sealed class CriarProjetoCommandHandler(IProjetoRepository projetos, IUnidadeDeTrabalho unidadeDeTrabalho, IUsuarioAtual usuarioAtual)
    : IRequestHandler<CriarProjetoCommand, ProjetoDto>
{
    public async Task<ProjetoDto> Handle(CriarProjetoCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        var nome = RequestValidator.ExigirTexto(request.Nome, "nome", 100);
        var descricao = RequestValidator.ValidarTextoOpcional(request.Descricao, "descricao", 255);
        var projeto = Projeto.Criar(nome, descricao);

        projetos.Adicionar(projeto);

        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        
        return projeto.ParaDto();
    }
}
