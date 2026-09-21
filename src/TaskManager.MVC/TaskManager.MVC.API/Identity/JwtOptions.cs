namespace TaskManager.MVC.API.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Emissor { get; init; } = string.Empty;
    public string Audiencia { get; init; } = string.Empty;
    public string ChaveSecreta { get; init; } = string.Empty;
}
