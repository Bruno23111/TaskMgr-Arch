namespace TaskManager.CleanArch.Infrastructure.Identity;

public sealed class GestorInicialOptions
{
    public const string NomeSecao = "GestorInicial";

    public string Nome { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Senha { get; init; } = string.Empty;

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(Nome) ||
        !string.IsNullOrWhiteSpace(Email) ||
        !string.IsNullOrWhiteSpace(Senha);

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Nome) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Senha))
            throw new InvalidOperationException(
                "As configurações GestorInicial:Nome, GestorInicial:Email e GestorInicial:Senha devem ser informadas juntas.");
    }
}
