using TaskManager.MVC.API.Configuration;
using Xunit;

namespace TaskManager.MVC.UnitTests.Infraestrutura;

/// <summary>No MVC, cada regra violada já sai como ApiException com o status HTTP e o código do contrato.</summary>
internal static class Erros
{
    public static void DeveFalhar(int status, string codigo, Action acao)
    {
        var erro = Assert.Throws<ApiException>(acao);
        Assert.Equal((status, codigo), (erro.Status, erro.Code));
    }

    public static async Task DeveFalharAsync(int status, string codigo, Func<Task> acao)
    {
        var erro = await Assert.ThrowsAsync<ApiException>(acao);
        Assert.Equal((status, codigo), (erro.Status, erro.Code));
    }
}
