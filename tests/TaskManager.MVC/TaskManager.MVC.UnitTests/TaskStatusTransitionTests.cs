using TaskManager.MVC.API.Models;
using TaskManager.MVC.API.Services.Tasks;
using TaskManager.MVC.UnitTests.Infraestrutura;
using Xunit;

namespace TaskManager.MVC.UnitTests;

/// <summary>
/// RN02 e RN04: a única regra do MVC isolada do DbContext. Matriz completa por categoria,
/// para Gestor e para quem não é Gestor, e os efeitos que a transição pede ao Service.
/// </summary>
public sealed class TaskStatusTransitionTests
{
    public static TheoryData<CategoriaStatus, CategoriaStatus, bool, int, string?> Matriz => new()
    {
        // origem, destino, solicitante é Gestor?, HTTP esperado, código de erro (null = permitida)
        { CategoriaStatus.Pendente, CategoriaStatus.Pendente, true, 200, null },
        { CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, true, 200, null },
        { CategoriaStatus.Pendente, CategoriaStatus.Concluida, true, 409, "TRANSICAO_INVALIDA" },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Pendente, true, 200, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento, true, 200, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, true, 200, null },
        { CategoriaStatus.Concluida, CategoriaStatus.Pendente, true, 200, null },
        { CategoriaStatus.Concluida, CategoriaStatus.EmAndamento, true, 409, "TRANSICAO_INVALIDA" },
        { CategoriaStatus.Concluida, CategoriaStatus.Concluida, true, 200, null },

        // Sem papel de Gestor: a reabertura vira 403 (CA09), as demais regras não mudam.
        { CategoriaStatus.Pendente, CategoriaStatus.Pendente, false, 200, null },
        { CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, false, 200, null },
        { CategoriaStatus.Pendente, CategoriaStatus.Concluida, false, 409, "TRANSICAO_INVALIDA" },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Pendente, false, 200, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento, false, 200, null },
        { CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, false, 200, null },
        { CategoriaStatus.Concluida, CategoriaStatus.Pendente, false, 403, "ACESSO_NEGADO" },
        { CategoriaStatus.Concluida, CategoriaStatus.EmAndamento, false, 409, "TRANSICAO_INVALIDA" },
        { CategoriaStatus.Concluida, CategoriaStatus.Concluida, false, 200, null },
    };

    [Theory]
    [Trait("RN", "RN02")]
    [MemberData(nameof(Matriz))]
    public void Transicao_SegueAMatrizDeCategorias(CategoriaStatus origem, CategoriaStatus destino, bool gestor, int http, string? codigo)
    {
        if (codigo is not null)
        {
            Erros.DeveFalhar(http, codigo, () => TaskStatusTransition.Evaluate(origem, destino, gestor));
            return;
        }

        TaskStatusTransition.Evaluate(origem, destino, gestor);
    }

    [Theory]
    [Trait("RN", "RN03")]
    [Trait("RN", "RN05")]
    [InlineData(CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, false, false, false)]
    [InlineData(CategoriaStatus.EmAndamento, CategoriaStatus.Pendente, false, false, false)]
    [InlineData(CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, true, true, false)]
    [InlineData(CategoriaStatus.Concluida, CategoriaStatus.Concluida, true, false, false)]
    [InlineData(CategoriaStatus.Concluida, CategoriaStatus.Pendente, false, false, true)]
    public void TransicaoPermitida_InformaOsEfeitosQueOServiceDeveAplicar(
        CategoriaStatus origem, CategoriaStatus destino, bool exigeResponsavel, bool preencheConclusao, bool limpaConclusao)
    {
        var transicao = TaskStatusTransition.Evaluate(origem, destino, isManager: true);

        Assert.Equal(new TaskStatusTransition(exigeResponsavel, preencheConclusao, limpaConclusao), transicao);
    }

    [Fact]
    [Trait("CA", "CA10")]
    public void ConcluidaParaEmAndamento_PorQuemNaoEhGestor_ApontaATransicaoAntesDaPermissao()
    {
        Erros.DeveFalhar(409, "TRANSICAO_INVALIDA",
            () => TaskStatusTransition.Evaluate(CategoriaStatus.Concluida, CategoriaStatus.EmAndamento, isManager: false));
    }
}
