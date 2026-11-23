using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TaskManager.ContractTests.Infraestrutura;

/// <summary>
/// WebApplicationFactory genérica: as duas APIs recebem exatamente a mesma configuração.
/// </summary>
internal sealed class ApiFactory<TEntryPoint>(IReadOnlyDictionary<string, string?> configuracao)
    : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Fora de Development: sem user-secrets da máquina do desenvolvedor e sem Swagger.
        builder.UseEnvironment("Testing");

        // UseSetting entra na configuração antes do Program.cs ler ConnectionStrings e Jwt.
        foreach (var (chave, valor) in configuracao)
        {
            builder.UseSetting(chave, valor);
        }
    }
}
