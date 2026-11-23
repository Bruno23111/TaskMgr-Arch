using System.Net;
using System.Text.Json;
using Xunit;
using Xunit.Sdk;

namespace TaskManager.ContractTests.Cliente;

/// <summary>Asserções de contrato: status HTTP, formato do corpo e código de erro.</summary>
public static class Verificacoes
{
    public static async Task<T> DeveRetornarAsync<T>(this Task<HttpResponseMessage> resposta, HttpStatusCode esperado) =>
        await (await resposta).DeveRetornarAsync<T>(esperado);

    public static async Task<T> DeveRetornarAsync<T>(this HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        var corpo = await LerCorpoAsync(resposta, esperado);
        Assert.Equal("application/json", resposta.Content.Headers.ContentType?.MediaType);
        return Desserializar<T>(corpo, Json.Contrato);
    }

    public static async Task DeveRetornarSemCorpoAsync(this Task<HttpResponseMessage> resposta, HttpStatusCode esperado) =>
        await (await resposta).DeveRetornarSemCorpoAsync(esperado);

    public static async Task DeveRetornarSemCorpoAsync(this HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        var corpo = await LerCorpoAsync(resposta, esperado);
        Assert.True(corpo.Length == 0, $"Era esperado corpo vazio, recebido: {corpo}");
    }

    public static async Task<Problema> DeveSerProblemaAsync(this Task<HttpResponseMessage> resposta, HttpStatusCode esperado, string codigo) =>
        await (await resposta).DeveSerProblemaAsync(esperado, codigo);

    public static async Task<Problema> DeveSerProblemaAsync(this HttpResponseMessage resposta, HttpStatusCode esperado, string codigo)
    {
        var corpo = await LerCorpoAsync(resposta, esperado);
        Assert.True(
            resposta.Content.Headers.ContentType?.MediaType == "application/problem+json",
            $"Erros devem usar application/problem+json; recebido '{resposta.Content.Headers.ContentType}': {corpo}");

        var problema = Desserializar<Problema>(corpo, Json.Problema);
        Assert.True(problema.Code == codigo, $"Código esperado {codigo}, recebido {problema.Code}: {corpo}");
        Assert.Equal((int)esperado, problema.Status);
        return problema;
    }

    private static async Task<string> LerCorpoAsync(HttpResponseMessage resposta, HttpStatusCode esperado)
    {
        var corpo = await resposta.Content.ReadAsStringAsync();
        if (resposta.StatusCode != esperado)
        {
            throw new XunitException(
                $"{resposta.RequestMessage?.Method} {resposta.RequestMessage?.RequestUri?.PathAndQuery}: " +
                $"esperado HTTP {(int)esperado}, recebido HTTP {(int)resposta.StatusCode}.{Environment.NewLine}{corpo}");
        }

        return corpo;
    }

    private static T Desserializar<T>(string corpo, JsonSerializerOptions opcoes)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(corpo, opcoes)
                ?? throw new XunitException($"Corpo nulo; esperado {typeof(T).Name}.");
        }
        catch (JsonException erro)
        {
            throw new XunitException($"Resposta fora do contrato ({typeof(T).Name}): {erro.Message}{Environment.NewLine}{corpo}");
        }
    }
}
