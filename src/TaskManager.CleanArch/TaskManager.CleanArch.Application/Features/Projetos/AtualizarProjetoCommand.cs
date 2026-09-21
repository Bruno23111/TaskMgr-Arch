using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Application.DTOs;

namespace TaskManager.CleanArch.Application.Features.Projetos;

public sealed record AtualizarProjetoCommand(Guid ProjetoId, string Nome, string? Descricao, int Versao) : IRequest<ProjetoDto>;

public sealed class AtualizarProjetoCommandHandler(IProjetoRepository projetos, IUnidadeDeTrabalho unidadeDeTrabalho, IUsuarioAtual usuarioAtual)
    : IRequestHandler<AtualizarProjetoCommand, ProjetoDto>
{
    public async Task<ProjetoDto> Handle(AtualizarProjetoCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        var nome = RequestValidator.ExigirTexto(request.Nome, "nome", 100);
        var descricao = RequestValidator.ValidarTextoOpcional(request.Descricao, "descricao", 255);
        var projeto = await projetos.ObterPorIdAsync(request.ProjetoId, cancellationToken)
            ?? throw new RecursoNaoEncontradoException("Projeto");
        RequestValidator.ExigirVersaoAtual(projeto.Versao, request.Versao);
        projeto.AtualizarDetalhes(nome, descricao);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        return projeto.ParaDto();
    }
}
