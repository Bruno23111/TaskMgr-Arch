using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using Xunit.Abstractions;

namespace TaskManager.E2E.Selenium.Infraestrutura;

// Encapsula o WebDriver: esperas explícitas, localizadores estáveis (name, aria-label, role) e capturas de tela.
public sealed class Navegador : IDisposable
{
    private static readonly TimeSpan Tempo = TimeSpan.FromSeconds(10);
    private readonly ITestOutputHelper saida;
    private readonly string pasta;
    private int capturas;

    public IWebDriver Driver { get; }

    public Navegador(ITestOutputHelper saida, string nomeDoTeste)
    {
        this.saida = saida;
        pasta = Path.Combine(Configuracao.PastaEvidencias, nomeDoTeste);
        Directory.CreateDirectory(pasta);
        Driver = Criar();
        Driver.Manage().Window.Size = new System.Drawing.Size(1366, 900);
    }

    private static IWebDriver Criar()
    {
        var argumentos = Configuracao.SemJanela ? new[] { "--headless=new", "--window-size=1366,900" } : Array.Empty<string>();
        switch (Configuracao.Navegador)
        {
            // Sem TASKMGR_DRIVER_DIR, o Selenium Manager baixa o driver compatível automaticamente.
            case "chrome":
                var chrome = new ChromeOptions(); chrome.AddArguments(argumentos);
                if (Environment.GetEnvironmentVariable("TASKMGR_NAVEGADOR_BIN") is { Length: > 0 } binario) chrome.BinaryLocation = binario;
                return Configuracao.PastaDoDriver is { } pc ? new ChromeDriver(ChromeDriverService.CreateDefaultService(pc), chrome) : new ChromeDriver(chrome);
            case "firefox":
                var firefox = new FirefoxOptions(); if (Configuracao.SemJanela) firefox.AddArgument("-headless");
                return Configuracao.PastaDoDriver is { } pf ? new FirefoxDriver(FirefoxDriverService.CreateDefaultService(pf), firefox) : new FirefoxDriver(firefox);
            default:
                var edge = new EdgeOptions(); edge.AddArguments(argumentos);
                return Configuracao.PastaDoDriver is { } pe ? new EdgeDriver(EdgeDriverService.CreateDefaultService(pe), edge) : new EdgeDriver(edge);
        }
    }

    public void Abrir(string caminho) => Driver.Navigate().GoToUrl(Configuracao.UrlSpa + caminho);

    public void EntrarComo(string email, string senha)
    {
        Abrir("/login");
        Preencher("email", email);
        Preencher("senha", senha);
        Driver.FindElement(By.CssSelector("button[type=submit]")).Click();
    }

    public void Preencher(string nomeDoCampo, string valor)
    {
        var campo = Esperar(d => d.FindElement(By.Name(nomeDoCampo)));
        campo.Clear();
        campo.SendKeys(valor);
    }

    public void ClicarBotao(string texto, string? naLinhaQueContem = null)
    {
        var escopo = naLinhaQueContem is null ? (ISearchContext)Driver : Linha(naLinhaQueContem);
        var botao = Esperar(_ => escopo.FindElements(By.TagName("button")).FirstOrDefault(b => b.Displayed && b.Text.Trim().EndsWith(texto)));
        botao.Click();
    }

    public void AbrirProjeto(string nome) =>
        Esperar(d => d.FindElements(By.CssSelector("a.link-projeto")).FirstOrDefault(a => a.Text.Trim() == nome)).Click();

    public IWebElement Linha(string contem) =>
        Esperar(d => d.FindElements(By.CssSelector("tbody tr")).FirstOrDefault(tr => tr.Text.Contains(contem)));

    public bool ExisteLinha(string contem) =>
        Driver.FindElements(By.CssSelector("tbody tr")).Any(tr => tr.Text.Contains(contem));

    public SelectElement Seletor(string rotulo) =>
        new(Esperar(d => d.FindElement(By.CssSelector($"select[aria-label=\"{rotulo}\"]"))));

    public void Escolher(string rotulo, string opcao) => Seletor(rotulo).SelectByText(opcao);

    public void EsperarOpcaoSelecionada(string rotulo, string opcao) =>
        Esperar(_ => Seletor(rotulo).SelectedOption.Text == opcao ? true : (bool?)null);

    public string EsperarAlerta() => Esperar(d => d.FindElements(By.CssSelector("[role=alert]")).FirstOrDefault(e => e.Displayed && e.Text.Length > 0)).Text.Trim();

    public bool HaAlerta() => Driver.FindElements(By.CssSelector("[role=alert]")).Any(e => e.Displayed);

    public void EsperarTexto(string texto) => Esperar(d => d.FindElement(By.TagName("body")).Text.Contains(texto) ? true : (bool?)null);

    public void EsperarTitulo(string texto) => Esperar(d => d.FindElements(By.TagName("h1")).Any(h => h.Text.Trim() == texto) ? true : (bool?)null);

    public void AceitarConfirmacao() => Esperar(d => { try { return d.SwitchTo().Alert(); } catch (NoAlertPresentException) { return null; } }).Accept();

    // WebDriverWait.Until repete enquanto a condição devolver null; o retorno final nunca é nulo.
    public T Esperar<T>(Func<IWebDriver, T?> condicao) where T : class => NovaEspera().Until(d => condicao(d))!;

    public bool Esperar(Func<IWebDriver, bool?> condicao) => NovaEspera().Until(d => condicao(d) == true);

    // A interface React recria elementos ao recarregar os dados; referências antigas são ignoradas até a condição valer.
    private WebDriverWait NovaEspera()
    {
        var espera = new WebDriverWait(Driver, Tempo);
        espera.IgnoreExceptionTypes(typeof(NotFoundException), typeof(StaleElementReferenceException));
        return espera;
    }

    public void Capturar(string etapa)
    {
        var arquivo = Path.Combine(pasta, $"{++capturas:00}-{etapa}.png");
        ((ITakesScreenshot)Driver).GetScreenshot().SaveAsFile(arquivo);
        saida.WriteLine($"captura: {arquivo}");
    }

    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
