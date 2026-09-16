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

    public static void ExigirVersao(this int versaoPersistida, int versaoEsperada)
    {
        versaoEsperada.ExigirVersaoInformada();
        if (versaoPersistida != versaoEsperada)
            throw new ConflitoDeConcorrenciaException();
    }

    public static void ExigirVersaoInformada(this int versao)
    {
        if (versao < 1)
            throw new ArgumentException("A versão deve ser maior que zero.", nameof(versao));
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
