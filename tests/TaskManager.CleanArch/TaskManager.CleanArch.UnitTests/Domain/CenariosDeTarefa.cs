using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.UnitTests.Domain;

/// <summary>
/// Tarefas em estados conhecidos. O Domain recebe as categorias já resolvidas,
/// então os IDs de status aqui são apenas identificadores, sem catálogo por trás.
/// </summary>
internal static class CenariosDeTarefa
{
    public static readonly DateTimeOffset Agora = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid ProjetoId = Guid.NewGuid();

    public static readonly Guid ResponsavelId = Guid.NewGuid();

    private static readonly Guid PendenteId = Guid.NewGuid();
    private static readonly Guid EmAndamentoId = Guid.NewGuid();
    private static readonly Guid ConcluidaId = Guid.NewGuid();

    /// <summary>ID do status do seed de cada categoria.</summary>
    public static Guid StatusDe(CategoriaStatus categoria) => categoria switch
    {
        CategoriaStatus.Pendente => PendenteId,
        CategoriaStatus.EmAndamento => EmAndamentoId,
        _ => ConcluidaId,
    };

    public static Tarefa Nova() => Tarefa.Criar(ProjetoId, "Escrever testes", "Descrição", PendenteId);

    /// <summary>Leva uma tarefa nova até a categoria pedida pelo caminho permitido da matriz.</summary>
    public static Tarefa Em(CategoriaStatus categoria, bool comResponsavel = true)
    {
        var tarefa = Nova();
        if (comResponsavel)
            tarefa.AtribuirResponsavel(ResponsavelId, CategoriaStatus.Pendente, responsavelAtivo: true);

        if (categoria is CategoriaStatus.EmAndamento or CategoriaStatus.Concluida)
            Mover(tarefa, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento);

        if (categoria is CategoriaStatus.Concluida)
            Mover(tarefa, CategoriaStatus.EmAndamento, CategoriaStatus.Concluida);

        tarefa.LimparDomainEvents();
        return tarefa;
    }

    public static void Mover(Tarefa tarefa, CategoriaStatus de, CategoriaStatus para, DateTimeOffset? quando = null) =>
        tarefa.AlterarStatus(StatusDe(para), de, para, responsavelAtivo: true, solicitanteEhGestor: true, quando ?? Agora);
}
