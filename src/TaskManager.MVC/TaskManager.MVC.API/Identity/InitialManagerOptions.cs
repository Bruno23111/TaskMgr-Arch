namespace TaskManager.MVC.API.Identity;

public sealed class InitialManagerOptions
{
    public const string SectionName = "GestorInicial";
    public string? Nome { get; init; }
    public string? Email { get; init; }
    public string? Senha { get; init; }
}
