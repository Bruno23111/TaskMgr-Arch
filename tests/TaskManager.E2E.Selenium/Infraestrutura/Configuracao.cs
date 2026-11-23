namespace TaskManager.E2E.Selenium.Infraestrutura;

// Valores lidos de variáveis de ambiente; os padrões correspondem ao ambiente local descrito no README.
public static class Configuracao
{
    public static string UrlSpa => Ler("TASKMGR_SPA_URL", "http://localhost:5173");
    public static string UrlApi => Ler("TASKMGR_API_URL", "http://localhost:5278");
    public static string EmailGestor => Ler("TASKMGR_GESTOR_EMAIL", "gestor@taskmgr.test");
    public static string SenhaGestor => Ler("TASKMGR_GESTOR_SENHA", "Teste2026");
    public static string Navegador => Ler("TASKMGR_NAVEGADOR", "edge");
    public static bool SemJanela => Ler("TASKMGR_SEM_JANELA", "true") != "false";
    public static string? PastaDoDriver => Environment.GetEnvironmentVariable("TASKMGR_DRIVER_DIR") is { Length: > 0 } pasta ? pasta : null;
    public static string PastaEvidencias => Ler("TASKMGR_EVIDENCIAS", Path.Combine(AppContext.BaseDirectory, "evidencias"));

    private static string Ler(string nome, string padrao) =>
        Environment.GetEnvironmentVariable(nome) is { Length: > 0 } valor ? valor : padrao;
}
