using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManager.ContractTests.Cliente;

internal static class Json
{
    /// <summary>Leitura estrita das representações de domínio definidas no contrato.</summary>
    public static readonly JsonSerializerOptions Contrato = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
    };

    /// <summary>
    /// ProblemDetails admite extensões (ex.: traceId do ASP.NET Core); o contrato só exige
    /// os campos obrigatórios e o code.
    /// </summary>
    public static readonly JsonSerializerOptions Problema = new(JsonSerializerDefaults.Web)
    {
        RespectNullableAnnotations = true,
    };

    public static readonly JsonSerializerOptions Envio = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
