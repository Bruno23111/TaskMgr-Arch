using TaskManager.CleanArch.Application.Abstractions.Identity;
using TaskManager.CleanArch.Application.Abstractions.Persistence;
using TaskManager.CleanArch.Application.Abstractions.Services;
using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Application.Common;

internal static class HandlerExtensions
{
    public static void ExigirAutenticado(this IUsuarioAtual usuarioAtual)
    {
        if (!usuarioAtual.Autenticado)
            throw new AcessoNegadoException();
    }

    public static void ExigirGestor(this IUsuarioAtual usuarioAtual)
    {
        if (!usuarioAtual.Autenticado || !usuarioAtual.Gestor)
            throw new AcessoNegadoException();
    }

    public static async Task SalvarDespacharAsync(this AggregateRoot agregado,
        IUnidadeDeTrabalho unidadeDeTrabalho,
        IDomainEventDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
        await dispatcher.DespacharAsync(agregado.LimparDomainEvents(), cancellationToken);
    }
}
