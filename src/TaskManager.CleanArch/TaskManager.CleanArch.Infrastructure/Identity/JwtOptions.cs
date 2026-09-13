namespace TaskManager.CleanArch.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string NomeSecao = "Jwt";

    public string Emissor { get; init; } = string.Empty;
    public string Audiencia { get; init; } = string.Empty;
    public string ChaveSecreta { get; init; } = string.Empty;
    public int DuracaoEmMinutos { get; init; } = 30;

    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Emissor))
            throw new InvalidOperationException("A configuração JWT 'Jwt:Emissor' é obrigatória.");

        if (string.IsNullOrWhiteSpace(Audiencia))
            throw new InvalidOperationException("A configuração JWT 'Jwt:Audiencia' é obrigatória.");

        if (string.IsNullOrWhiteSpace(ChaveSecreta) || ChaveSecreta.Length < 32)
            throw new InvalidOperationException("A configuração JWT 'Jwt:ChaveSecreta' deve ter pelo menos 32 caracteres.");

        if (DuracaoEmMinutos <= 0)
            throw new InvalidOperationException("A configuração JWT 'Jwt:DuracaoEmMinutos' deve ser maior que zero.");
    }
}
