using MediatR;
using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Application.Common;

namespace TaskManager.CleanArch.Application.Features.Projetos;

public sealed record ExcluirProjetoCommand(Guid ProjetoId, int Versao) : IRequest;

public sealed class ExcluirProjetoCommandHandler(IProjetoRepository projetos, ITarefaRepository tarefas, IUnidadeDeTrabalho unidadeDeTrabalho, IUsuarioAtual usuarioAtual, IRelogio relogio) : IRequestHandler<ExcluirProjetoCommand>
{
    public async Task Handle(ExcluirProjetoCommand request, CancellationToken cancellationToken)
    {
        usuarioAtual.ExigirGestor();
        RequestValidator.ExigirVersaoInformada(request.Versao);
        await unidadeDeTrabalho.ExecutarEmTransacaoSerializavelAsync(async token =>
        {
            if (!await projetos.BloquearParaAtualizacaoAsync(request.ProjetoId, token))
                throw new RecursoNaoEncontradoException("Projeto");

            var projeto = await projetos.ObterPorIdAsync(request.ProjetoId, token) ?? throw new RecursoNaoEncontradoException("Projeto");
            RequestValidator.ExigirVersaoAtual(projeto.Versao, request.Versao);
            if (await projetos.PossuiTarefaEmAndamentoAsync(projeto.Id, token))
                throw new ProjetoComTarefaEmAndamentoException();
            var agora = relogio.DataHoraAgoraUTC;
            foreach (var tarefa in await tarefas.ListarVisiveisPorProjetoAsync(projeto.Id, token)) tarefa.ExcluirComProjeto(agora);
            projeto.Excluir(agora);
            await unidadeDeTrabalho.SalvarAlteracoesAsync(token);
            return true;
        }, cancellationToken);
    }
}
