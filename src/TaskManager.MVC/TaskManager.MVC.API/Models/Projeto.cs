namespace TaskManager.MVC.API.Models;

public sealed class Projeto : IVersionado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nome { get; set; } = null!;
    public string? Descricao { get; set; }
    public int Versao { get; set; } = 1;
    public DateTimeOffset? ExcluidoEm { get; set; }
}
