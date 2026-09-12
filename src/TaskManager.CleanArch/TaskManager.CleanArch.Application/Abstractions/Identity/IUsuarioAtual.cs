namespace TaskManager.CleanArch.Application.Abstractions.Identity;

public interface IUsuarioAtual
{
    string UsuarioId { get; }
    bool Autenticado { get; }
    bool Gestor { get; }
}
