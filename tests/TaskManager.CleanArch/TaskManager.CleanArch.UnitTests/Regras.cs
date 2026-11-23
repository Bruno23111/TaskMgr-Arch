using TaskManager.CleanArch.Domain.Common;
using Xunit;

namespace TaskManager.CleanArch.UnitTests;

/// <summary>Asserções de violação de regra de domínio pelo código público (o mesmo do contrato HTTP).</summary>
internal static class Regras
{
    public static void DeveViolar(string codigo, Action acao)
    {
        var erro = Assert.Throws<DomainRuleViolationException>(acao);
        Assert.Equal(codigo, erro.Code);
    }

    public static async Task DeveViolarAsync(string codigo, Func<Task> acao)
    {
        var erro = await Assert.ThrowsAsync<DomainRuleViolationException>(acao);
        Assert.Equal(codigo, erro.Code);
    }
}
