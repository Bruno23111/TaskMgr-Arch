using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Domain.Entities;

public sealed class Projeto
{
    private Projeto()
    {
    }

    private Projeto(Guid id, string nome, string? descricao)
    {
        Id = id;
        AtualizarDetalhes(nome, descricao, incrementaVersao: false);
        Versao = 1;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public int Versao { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }

    public static Projeto Criar(string nome, string? descricao) => new(Guid.NewGuid(), nome, descricao);

    public void AtualizarDetalhes(string nome, string? descricao)
    {
        GarantirAtivo();
        AtualizarDetalhes(nome, descricao, incrementaVersao: true);
    }

    public void Excluir(DateTimeOffset agoraUtc)
    {
        GarantirAtivo();
        ExcluidoEm = GarantirUtc(agoraUtc);
        Versao++;
    }

    private void AtualizarDetalhes(string nome, string? descricao, bool incrementaVersao)
    {
        GarantirTexto(nome, nameof(nome), 100);
        GarantirTamanho(descricao, nameof(descricao), 255);
        Nome = nome.Trim();
        Descricao = NormalizarOpcional(descricao);
        if (incrementaVersao)
            Versao++;
    }

    private void GarantirAtivo()
    {
        if (ExcluidoEm is not null)
            throw new DomainRuleViolationException("RECURSO_EXCLUIDO", "O projeto foi excluído.");
    }

    internal static void GarantirTexto(string valor, string nome, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException("O campo é obrigatório.", nome);
        GarantirTamanho(valor, nome, tamanhoMaximo);
    }

    internal static void GarantirTamanho(string? valor, string nome, int tamanhoMaximo)
    {
        if (valor?.Trim().Length > tamanhoMaximo)
            throw new ArgumentException($"O campo deve ter no máximo {tamanhoMaximo} caracteres.", nome);
    }

    internal static string? NormalizarOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    internal static DateTimeOffset GarantirUtc(DateTimeOffset valor) => valor.Offset == TimeSpan.Zero
        ? valor
        : throw new ArgumentException("A data deve estar em UTC.", nameof(valor));
}
