using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace TaskManager.ContractTests.Cliente;

/// <summary>
/// Cliente HTTP mínimo: conhece apenas rotas, corpo JSON e token. Instâncias autenticadas
/// compartilham o mesmo HttpClient e enviam o token por requisição, o que permite
/// chamadas concorrentes de usuários diferentes (CA26–CA28).
/// </summary>
public sealed class ClienteApi(HttpClient http, string? token = null)
{
    public string? Token { get; } = token;

    public ClienteApi ComToken(string? novoToken) => new(http, novoToken);

    public Task<HttpResponseMessage> GetAsync(string rota) => EnviarAsync(HttpMethod.Get, rota);

    public Task<HttpResponseMessage> PostAsync(string rota, object? corpo) => EnviarAsync(HttpMethod.Post, rota, corpo);

    public Task<HttpResponseMessage> PutAsync(string rota, object? corpo) => EnviarAsync(HttpMethod.Put, rota, corpo);

    public Task<HttpResponseMessage> PatchAsync(string rota, object? corpo) => EnviarAsync(HttpMethod.Patch, rota, corpo);

    public Task<HttpResponseMessage> DeleteAsync(string rota) => EnviarAsync(HttpMethod.Delete, rota);

    /// <summary>Envia JSON cru, para cenários de corpo malformado ou com campos fora do contrato.</summary>
    public Task<HttpResponseMessage> EnviarJsonCruAsync(HttpMethod metodo, string rota, string json)
    {
        var requisicao = NovaRequisicao(metodo, rota);
        requisicao.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return http.SendAsync(requisicao);
    }

    public async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string rota, object? corpo = null)
    {
        var requisicao = NovaRequisicao(metodo, rota);
        if (corpo is not null)
        {
            requisicao.Content = JsonContent.Create(corpo, corpo.GetType(), options: Json.Envio);
        }

        return await http.SendAsync(requisicao);
    }

    private HttpRequestMessage NovaRequisicao(HttpMethod metodo, string rota)
    {
        var requisicao = new HttpRequestMessage(metodo, rota);
        if (Token is not null)
        {
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        return requisicao;
    }
}

public static class Rotas
{
    public const string Login = "/auth/login";
    public const string Projetos = "/projetos";
    public const string Tarefas = "/tarefas";
    public const string Status = "/status";
    public const string Responsaveis = "/responsaveis";

    public static string Projeto(Guid id) => $"/projetos/{id}";

    public static string ProjetoComVersao(Guid id, int versao) => $"/projetos/{id}?versao={versao}";

    public static string Tarefa(Guid id) => $"/tarefas/{id}";

    public static string TarefaComVersao(Guid id, int versao) => $"/tarefas/{id}?versao={versao}";

    public static string TarefasDoProjeto(Guid projetoId) => $"/tarefas?projetoId={projetoId}&tamanho=100";

    public static string StatusDaTarefa(Guid id) => $"/tarefas/{id}/status";

    public static string ResponsavelDaTarefa(Guid id) => $"/tarefas/{id}/responsavel";

    public static string ResponsavelDaTarefaComVersao(Guid id, int versao) => $"/tarefas/{id}/responsavel?versao={versao}";

    public static string StatusItem(Guid id) => $"/status/{id}";

    public static string StatusComVersao(Guid id, int versao) => $"/status/{id}?versao={versao}";

    public static string Responsavel(Guid id) => $"/responsaveis/{id}";

    public static string ResponsavelComVersao(Guid id, int versao) => $"/responsaveis/{id}?versao={versao}";

    public static string Desativacao(Guid id) => $"/responsaveis/{id}/desativacao";
}
