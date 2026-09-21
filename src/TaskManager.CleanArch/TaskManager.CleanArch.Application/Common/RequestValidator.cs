using System.Net.Mail;

namespace TaskManager.CleanArch.Application.Common;

internal static class RequestValidator
{
    public static void ExigirVersaoInformada(int versao)
    {
        if (versao < 1)
            throw new ArgumentException("A versão deve ser maior que zero.", nameof(versao));
    }

    public static void ExigirVersaoAtual(int versaoPersistida, int versaoEsperada)
    {
        ExigirVersaoInformada(versaoEsperada);
        if (versaoPersistida != versaoEsperada)
            throw new ConflitoDeConcorrenciaException();
    }

    public static string ExigirTexto(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException($"O campo {campo} é obrigatório.", campo);

        var normalizado = valor.Trim();
        if (normalizado.Length > tamanhoMaximo)
            throw new ArgumentException($"O campo {campo} deve ter no máximo {tamanhoMaximo} caracteres.", campo);

        return normalizado;
    }

    public static string ExigirValor(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ArgumentException($"O campo {campo} é obrigatório.", campo);

        return valor;
    }

    public static string? ValidarTextoOpcional(string? valor, string campo, int tamanhoMaximo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;

        var normalizado = valor.Trim();
        if (normalizado.Length > tamanhoMaximo)
            throw new ArgumentException($"O campo {campo} deve ter no máximo {tamanhoMaximo} caracteres.", campo);

        return normalizado;
    }

    public static string ExigirEmail(string? valor)
    {
        var email = ExigirTexto(valor, "email", 256);
        if (!MailAddress.TryCreate(email, out var endereco) ||
            !string.Equals(endereco.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("O e-mail informado é inválido.", nameof(valor));
        }

        return email;
    }

    public static void ExigirIdInformado(Guid valor, string campo)
    {
        if (valor == Guid.Empty)
            throw new ArgumentException($"O campo {campo} é obrigatório.", campo);
    }
}
