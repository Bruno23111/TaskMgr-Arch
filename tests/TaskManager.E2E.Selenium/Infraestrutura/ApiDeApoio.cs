using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManager.E2E.Selenium.Infraestrutura;

// Prepara os dados de cada teste pela API, para que os testes não dependam uns dos outros nem de dados permanentes.
public sealed class ApiDeApoio : IDisposable
{
    public const string StatusEmAndamento = "22222222-2222-2222-2222-222222222222";

    private readonly HttpClient http = new() { BaseAddress = new Uri(Configuracao.UrlApi) };

    public static string Sufixo() => Guid.NewGuid().ToString("N")[..8];

    public async Task<string> EntrarAsync(string email, string senha)
    {
        var resposta = await http.PostAsJsonAsync("/auth/login", new { email, senha });
        resposta.EnsureSuccessStatusCode();
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
    }

    public async Task<JsonElement> CriarProjetoAsync(string token, string nome) =>
        await EnviarAsync(token, HttpMethod.Post, "/projetos", new { nome, descricao = "Criado pelo teste Selenium" });

    public async Task<JsonElement> CriarTarefaAsync(string token, string projetoId, string titulo) =>
        await EnviarAsync(token, HttpMethod.Post, "/tarefas", new { titulo, projetoId });

    public async Task<JsonElement> IniciarTarefaAsync(string token, JsonElement tarefa) =>
        await EnviarAsync(token, HttpMethod.Patch, $"/tarefas/{tarefa.GetProperty("id").GetString()}/status",
            new { statusId = StatusEmAndamento, versao = tarefa.GetProperty("versao").GetInt32() });

    public async Task<JsonElement> CriarContaAsync(string token, string nome, string email, string senha, string papel) =>
        await EnviarAsync(token, HttpMethod.Post, "/responsaveis", new { nome, email, senha, papel });

    private async Task<JsonElement> EnviarAsync(string token, HttpMethod metodo, string caminho, object corpo)
    {
        using var requisicao = new HttpRequestMessage(metodo, caminho) { Content = JsonContent.Create(corpo) };
        requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resposta = await http.SendAsync(requisicao);
        var texto = await resposta.Content.ReadAsStringAsync();
        if (!resposta.IsSuccessStatusCode)
            throw new InvalidOperationException($"{metodo} {caminho} -> {(int)resposta.StatusCode}: {texto}");
        return JsonDocument.Parse(texto).RootElement.Clone();
    }

    public void Dispose() => http.Dispose();
}
