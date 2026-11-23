using TaskManager.CleanArch.Domain.Enums;
using Xunit;
using static TaskManager.CleanArch.UnitTests.Domain.CenariosDeTarefa;

namespace TaskManager.CleanArch.UnitTests.Domain;

/// <summary>
/// RN02 e RN04: matriz completa de transições por categoria, para Gestor e para quem não é Gestor.
/// O destino é sempre um status diferente do atual (um status adicional da categoria),
/// para exercitar a regra e não o atalho idempotente.
/// </summary>
public sealed class MatrizDeTransicaoTests
{
    private const string TransicaoInvalida = "TRANSICAO_INVALIDA";

    public static TheoryData<CategoriaStatus, CategoriaStatus, bool, string?> Matriz => new()
    {
        // origem, destino, solicitante é Gestor?, código esperado (null = permitida)
        { CategoriaStatus.Pendente, CategoriaStatus.Pendente, true, null },
        { CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, true, null },
        { CategoriaStatus.Pendente, CategoriaStatus.Concluida, true, TransicaoInvalida },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Pendente, true, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento, true, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, true, null },
        { CategoriaStatus.Concluida, CategoriaStatus.Pendente, true, null },
        { CategoriaStatus.Concluida, CategoriaStatus.EmAndamento, true, TransicaoInvalida },
        { CategoriaStatus.Concluida, CategoriaStatus.Concluida, true, null },

        // Sem papel de Gestor, a única diferença é a reabertura (RN04).
        { CategoriaStatus.Pendente, CategoriaStatus.Pendente, false, null },
        { CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, false, null },
        { CategoriaStatus.Pendente, CategoriaStatus.Concluida, false, TransicaoInvalida },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Pendente, false, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento, false, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, false, null },
        { CategoriaStatus.Concluida, CategoriaStatus.Pendente, false, TransicaoInvalida },
        { CategoriaStatus.Concluida, CategoriaStatus.EmAndamento, false, TransicaoInvalida },
        { CategoriaStatus.Concluida, CategoriaStatus.Concluida, false, null },
    };

    [Theory]
    [Trait("RN", "RN02")]
    [MemberData(nameof(Matriz))]
    public void Transicao_SegueAMatrizDeCategorias(CategoriaStatus origem, CategoriaStatus destino, bool gestor, string? codigo)
    {
        var tarefa = Em(origem);
        var versao = tarefa.Versao;
        var statusDeOrigem = tarefa.StatusId;
        var statusAdicional = Guid.NewGuid();

        void Alterar() => tarefa.AlterarStatus(statusAdicional, origem, destino, responsavelAtivo: true, gestor, Agora);

        if (codigo is not null)
        {
            Regras.DeveViolar(codigo, Alterar);
            Assert.Equal(statusDeOrigem, tarefa.StatusId);
            Assert.Equal(versao, tarefa.Versao);
            Assert.Empty(tarefa.DomainEvents);
            return;
        }

        Alterar();
        Assert.Equal(statusAdicional, tarefa.StatusId);
        Assert.Equal(versao + 1, tarefa.Versao);
    }
}
