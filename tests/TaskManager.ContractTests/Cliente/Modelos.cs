using System.Text.Json.Serialization;

namespace TaskManager.ContractTests.Cliente;

// Representações escritas a partir de docs/openapi/taskmgr-v1.yaml, sem reutilizar tipos das APIs.
// A desserialização é estrita (ver Json.Contrato): campo fora do contrato, campo [JsonRequired]
// ausente ou null em campo não anulável falham o teste. [JsonRequired] espelha a lista
// "required" de cada schema; os demais campos podem ser omitidos pela API.

public sealed record LoginResposta(
    [property: JsonRequired] string AccessToken,
    [property: JsonRequired] DateTimeOffset ExpiraEm);

public sealed record Projeto(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] string Nome,
    string? Descricao,
    [property: JsonRequired] int Versao);

public sealed record Tarefa(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] Guid ProjetoId,
    [property: JsonRequired] Guid StatusId,
    Guid? ResponsavelId,
    [property: JsonRequired] string Titulo,
    string? Descricao,
    DateTimeOffset? ConcluidaEm,
    [property: JsonRequired] int Versao)
{
    // As duas APIs devolvem ConcluidaEm com 100 ns de precisão logo após a gravação, mas o
    // PostgreSQL guarda microssegundos: uma leitura posterior difere na 7ª casa decimal.
    // A comparação ignora essa diferença, que não tem significado para o contrato.
    public bool Equals(Tarefa? outra) =>
        outra is not null
        && (Id, ProjetoId, StatusId, ResponsavelId, Titulo, Descricao, Versao)
            == (outra.Id, outra.ProjetoId, outra.StatusId, outra.ResponsavelId, outra.Titulo, outra.Descricao, outra.Versao)
        && Microssegundos(ConcluidaEm) == Microssegundos(outra.ConcluidaEm);

    public override int GetHashCode() => HashCode.Combine(Id, StatusId, Versao);

    public static long? Microssegundos(DateTimeOffset? data) => data?.UtcTicks / 10;
}

public sealed record StatusTarefa(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] string Codigo,
    [property: JsonRequired] string Nome,
    [property: JsonRequired] string Categoria,
    [property: JsonRequired] bool Protegido,
    [property: JsonRequired] int Versao);

public sealed record Responsavel(
    [property: JsonRequired] Guid Id,
    [property: JsonRequired] string UsuarioId,
    [property: JsonRequired] string Nome,
    [property: JsonRequired] string Email,
    [property: JsonRequired] bool Ativo,
    [property: JsonRequired] string Papel,
    [property: JsonRequired] int Versao);

public sealed record Paginado<T>(
    [property: JsonRequired] IReadOnlyList<T> Itens,
    [property: JsonRequired] int Pagina,
    [property: JsonRequired] int Tamanho,
    [property: JsonRequired] int Total)
{
    // Records comparam listas por referência; esta igualdade compara os itens.
    public bool Equals(Paginado<T>? outro) =>
        outro is not null && (Pagina, Tamanho, Total) == (outro.Pagina, outro.Tamanho, outro.Total) && Itens.SequenceEqual(outro.Itens);

    public override int GetHashCode() => HashCode.Combine(Pagina, Tamanho, Total);
}

public sealed record Problema(
    [property: JsonRequired] string Type,
    [property: JsonRequired] string Title,
    [property: JsonRequired] int Status,
    string? Detail,
    string? Instance,
    [property: JsonRequired] string Code,
    IReadOnlyDictionary<string, string[]>? Errors);

public static class Categorias
{
    public const string Pendente = "PENDENTE";
    public const string EmAndamento = "EM_ANDAMENTO";
    public const string Concluida = "CONCLUIDA";
}

public static class Papeis
{
    public const string Usuario = "Usuario";
    public const string Gestor = "Gestor";
}

public static class Codigos
{
    public const string DadosInvalidos = "DADOS_INVALIDOS";
    public const string NaoAutenticado = "NAO_AUTENTICADO";
    public const string AcessoNegado = "ACESSO_NEGADO";
    public const string RecursoNaoEncontrado = "RECURSO_NAO_ENCONTRADO";
    public const string TransicaoInvalida = "TRANSICAO_INVALIDA";
    public const string ResponsavelObrigatorio = "RESPONSAVEL_OBRIGATORIO";
    public const string ProjetoComTarefaEmAndamento = "PROJETO_COM_TAREFA_EM_ANDAMENTO";
    public const string RegistroEmUso = "REGISTRO_EM_USO";
    public const string TarefaConcluida = "TAREFA_CONCLUIDA";
    public const string RegistroProtegido = "REGISTRO_PROTEGIDO";
    public const string ResponsavelInativo = "RESPONSAVEL_INATIVO";
    public const string ValorDuplicado = "VALOR_DUPLICADO";
    public const string Concorrencia = "CONCORRENCIA";
}
