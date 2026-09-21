using System.Net.Mail;

namespace TaskManager.MVC.API.Configuration;

internal static class RequestValidation
{
    public static string RequiredValue(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"O campo {field} é obrigatório.");
        }

        return value;
    }

    public static string RequiredText(string? value, string field, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Invalid($"O campo {field} é obrigatório.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw Invalid($"O campo {field} deve ter no máximo {maximumLength} caracteres.");
        }

        return normalized;
    }

    public static string? OptionalText(string? value, string field, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw Invalid($"O campo {field} deve ter no máximo {maximumLength} caracteres.");
        }

        return normalized;
    }

    public static string Email(string? value)
    {
        var email = RequiredText(value, "email", 256);
        if (!MailAddress.TryCreate(email, out var address) ||
            !string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase))
        {
            throw Invalid("O e-mail informado é inválido.");
        }

        return email;
    }

    public static void Version(int value)
    {
        if (value < 1)
        {
            throw Invalid("A versão deve ser maior que zero.");
        }
    }

    public static void RequiredId(Guid value, string field)
    {
        if (value == Guid.Empty)
        {
            throw Invalid($"O campo {field} é obrigatório.");
        }
    }

    public static ApiException Invalid(string detail) =>
        new(400, "DADOS_INVALIDOS", "Dados inválidos.", detail);
}
