namespace TaskManager.MVC.API.Models;

public enum CategoriaStatus
{
    Pendente,
    EmAndamento,
    Concluida
}

public sealed class StatusTarefa : IVersionado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Codigo { get; set; } = null!;
    public string Nome { get; set; } = null!;
    public CategoriaStatus Categoria { get; set; }
    public bool Protegido { get; set; }
    public int Versao { get; set; } = 1;
}
