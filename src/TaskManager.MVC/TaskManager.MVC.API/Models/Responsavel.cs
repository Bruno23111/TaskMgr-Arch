namespace TaskManager.MVC.API.Models;

public sealed class Responsavel : IVersionado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UsuarioId { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public bool Ativo { get; set; } = true;
    public int Versao { get; set; } = 1;
}
