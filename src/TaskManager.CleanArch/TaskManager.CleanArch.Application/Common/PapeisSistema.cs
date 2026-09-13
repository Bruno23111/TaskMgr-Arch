namespace TaskManager.CleanArch.Application.Common;

public static class PapeisSistema
{
    public const string Usuario = "Usuario";
    public const string Gestor = "Gestor";

    public static bool Valido(string papel) =>
        string.Equals(papel, Usuario, StringComparison.Ordinal) ||
        string.Equals(papel, Gestor, StringComparison.Ordinal);
}
