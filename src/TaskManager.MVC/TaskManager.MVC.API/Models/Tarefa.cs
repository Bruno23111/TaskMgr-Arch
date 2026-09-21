namespace TaskManager.MVC.API.Models;

public sealed class Tarefa : IVersionado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjetoId { get; set; }
    public Guid StatusId { get; set; }
    public Guid? ResponsavelId { get; set; }
    public string Titulo { get; set; } = null!;
    public string? Descricao { get; set; }
    public DateTimeOffset? ConcluidaEm { get; set; }
    public DateTimeOffset? ExcluidoEm { get; set; }
    public int Versao { get; set; } = 1;
}
