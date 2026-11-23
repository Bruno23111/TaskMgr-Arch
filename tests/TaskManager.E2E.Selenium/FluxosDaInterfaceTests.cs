using System.Runtime.CompilerServices;
using OpenQA.Selenium;
using TaskManager.E2E.Selenium.Infraestrutura;
using Xunit;
using Xunit.Abstractions;

namespace TaskManager.E2E.Selenium;

// Fluxos ponta a ponta da interface React. Cada teste indica o caso do Apêndice E que automatiza
// e prepara os próprios dados pela API (nomes com sufixo aleatório), sem depender da ordem de execução.
public sealed class FluxosDaInterfaceTests(ITestOutputHelper saida) : IDisposable
{
    private readonly ApiDeApoio api = new();

    [Fact] // CT-01
    public Task SE01_GestorEntraVeOsProjetosESai() => Cenario(async n =>
    {
        var projeto = $"SE01 {ApiDeApoio.Sufixo()}";
        await api.CriarProjetoAsync(await TokenGestor(), projeto);

        n.EntrarComo(Configuracao.EmailGestor, Configuracao.SenhaGestor);
        n.EsperarTitulo("Projetos");
        n.EsperarTexto(projeto);
        var cabecalho = n.Driver.FindElement(By.CssSelector("header.topo")).Text;
        Assert.Contains(Configuracao.EmailGestor, cabecalho);
        Assert.Contains("Gestor", cabecalho);
        n.Capturar("projetos");

        n.Driver.Navigate().Refresh();
        n.EsperarTitulo("Projetos");

        n.ClicarBotao("Sair");
        n.EsperarTexto("Entrar no TaskMgr");
        Assert.EndsWith("/login", new Uri(n.Driver.Url).AbsolutePath);
        n.Capturar("apos-sair");
    });

    [Theory] // CT-02
    [InlineData("senha-errada", null, "Errada2026")]
    [InlineData("email-inexistente", "naoexiste@taskmgr.test", null)]
    public Task SE02_LoginInvalidoMostraMensagemPadraoENaoCriaSessao(string classe, string? email, string? senha) => Cenario(n =>
    {
        n.EntrarComo(email ?? Configuracao.EmailGestor, senha ?? Configuracao.SenhaGestor);

        Assert.Equal("Sessão inválida ou credenciais incorretas.", n.EsperarAlerta());
        Assert.EndsWith("/login", new Uri(n.Driver.Url).AbsolutePath);
        Assert.Null(((IJavaScriptExecutor)n.Driver).ExecuteScript("return localStorage.getItem('taskmgr.sessao');"));
        n.Capturar(classe);
        return Task.CompletedTask;
    });

    [Fact] // CT-03
    public Task SE03_GestorCriaEEditaProjeto() => Cenario(n =>
    {
        var nome = $"SE03 {ApiDeApoio.Sufixo()}";
        n.EntrarComo(Configuracao.EmailGestor, Configuracao.SenhaGestor);
        n.EsperarTitulo("Projetos");

        n.ClicarBotao("Novo projeto");
        n.Preencher("nome", nome);
        n.Preencher("descricao", "Criado pela interface");
        n.ClicarBotao("Criar projeto");
        Assert.Contains("Criado pela interface", n.Linha(nome).Text);
        n.Capturar("criado");

        n.ClicarBotao("Editar", naLinhaQueContem: nome);
        n.Preencher("nome", nome + " editado");
        n.ClicarBotao("Salvar alterações");
        n.Linha(nome + " editado");

        n.Driver.Navigate().Refresh();
        n.Linha(nome + " editado");
        Assert.False(n.HaAlerta());
        n.Capturar("editado");
        return Task.CompletedTask;
    });

    [Fact] // CT-07, CT-14 e CT-12
    public Task SE04_CicloCompletoDaTarefa() => Cenario(async n =>
    {
        var sufixo = ApiDeApoio.Sufixo();
        var token = await TokenGestor();
        var projeto = $"SE04 {sufixo}";
        var responsavel = $"Resp {sufixo}";
        await api.CriarProjetoAsync(token, projeto);
        await api.CriarContaAsync(token, responsavel, $"resp-{sufixo}@taskmgr.test", "Teste2026", "Usuario");

        n.EntrarComo(Configuracao.EmailGestor, Configuracao.SenhaGestor);
        n.AbrirProjeto(projeto);
        n.EsperarTexto("Nenhuma tarefa neste projeto");

        n.ClicarBotao("Nova tarefa");
        n.Preencher("titulo", "Tarefa do ciclo");
        n.ClicarBotao("Criar tarefa");
        n.EsperarOpcaoSelecionada("Status de Tarefa do ciclo", "Pendente");
        Assert.Equal("Sem responsável", n.Seletor("Responsável de Tarefa do ciclo").SelectedOption.Text);
        n.Capturar("criada-pendente");

        n.Escolher("Responsável de Tarefa do ciclo", responsavel);
        n.EsperarOpcaoSelecionada("Responsável de Tarefa do ciclo", responsavel);

        n.Escolher("Status de Tarefa do ciclo", "Em andamento");
        n.EsperarOpcaoSelecionada("Status de Tarefa do ciclo", "Em andamento");

        n.Escolher("Status de Tarefa do ciclo", "Concluída");
        n.EsperarOpcaoSelecionada("Status de Tarefa do ciclo", "Concluída");
        n.Esperar(_ => n.Linha("Tarefa do ciclo").Text.Contains("Concluída em") ? true : (bool?)null);
        Assert.False(n.Seletor("Responsável de Tarefa do ciclo").WrappedElement.Enabled);
        n.Capturar("concluida");

        n.Escolher("Status de Tarefa do ciclo", "Pendente");
        n.EsperarOpcaoSelecionada("Status de Tarefa do ciclo", "Pendente");
        n.Esperar(_ => !n.Linha("Tarefa do ciclo").Text.Contains("Concluída em") ? true : (bool?)null);
        Assert.Equal(responsavel, n.Seletor("Responsável de Tarefa do ciclo").SelectedOption.Text);
        Assert.False(n.HaAlerta());
        n.Capturar("reaberta");
    });

    [Fact] // CT-11
    public Task SE05_ConcluirSemResponsavelMostraAlertaEMantemOStatus() => Cenario(async n =>
    {
        var token = await TokenGestor();
        var projeto = $"SE05 {ApiDeApoio.Sufixo()}";
        var id = (await api.CriarProjetoAsync(token, projeto)).GetProperty("id").GetString()!;
        await api.IniciarTarefaAsync(token, await api.CriarTarefaAsync(token, id, "Sem responsável"));

        n.EntrarComo(Configuracao.EmailGestor, Configuracao.SenhaGestor);
        n.AbrirProjeto(projeto);
        n.Escolher("Status de Sem responsável", "Concluída");

        Assert.Equal("Atribua um responsável ativo antes de concluir a tarefa.", n.EsperarAlerta());
        n.EsperarOpcaoSelecionada("Status de Sem responsável", "Em andamento");
        Assert.DoesNotContain("Concluída em", n.Linha("Sem responsável").Text);
        n.Capturar("alerta");
    });

    [Fact] // CT-05
    public Task SE06_ExclusaoDeProjetoComTarefaEmAndamentoEhBloqueada() => Cenario(async n =>
    {
        var token = await TokenGestor();
        var projeto = $"SE06 {ApiDeApoio.Sufixo()}";
        var id = (await api.CriarProjetoAsync(token, projeto)).GetProperty("id").GetString()!;
        await api.IniciarTarefaAsync(token, await api.CriarTarefaAsync(token, id, "Em execução"));

        n.EntrarComo(Configuracao.EmailGestor, Configuracao.SenhaGestor);
        n.EsperarTitulo("Projetos");
        n.ClicarBotao("Excluir", naLinhaQueContem: projeto);
        n.AceitarConfirmacao();

        Assert.Equal("O projeto tem tarefas em andamento e não pode ser excluído.", n.EsperarAlerta());
        n.Linha(projeto);
        n.Capturar("bloqueado");
    });

    [Fact] // CT-21
    public Task SE07_UsuarioNaoVeAcoesExclusivasDoGestor() => Cenario(async n =>
    {
        var sufixo = ApiDeApoio.Sufixo();
        var token = await TokenGestor();
        var projeto = $"SE07 {sufixo}";
        var email = $"usuario-{sufixo}@taskmgr.test";
        var id = (await api.CriarProjetoAsync(token, projeto)).GetProperty("id").GetString()!;
        await api.CriarTarefaAsync(token, id, "Tarefa de outra pessoa");
        await api.CriarContaAsync(token, $"Usuário {sufixo}", email, "Teste2026", "Usuario");

        n.EntrarComo(email, "Teste2026");
        n.EsperarTitulo("Projetos");
        n.Linha(projeto);
        var botoes = n.Driver.FindElements(By.TagName("button")).Select(b => b.Text.Trim()).ToArray();
        Assert.DoesNotContain(botoes, b => b.EndsWith("Novo projeto") || b.EndsWith("Editar") || b.EndsWith("Excluir"));
        n.Capturar("projetos-usuario");

        n.AbrirProjeto(projeto);
        Assert.False(n.Seletor("Status de Tarefa de outra pessoa").WrappedElement.Enabled);
        Assert.False(n.Seletor("Responsável de Tarefa de outra pessoa").WrappedElement.Enabled);
        var botoesDaTarefa = n.Driver.FindElements(By.TagName("button")).Select(b => b.Text.Trim()).ToArray();
        Assert.DoesNotContain(botoesDaTarefa, b => b.EndsWith("Nova tarefa") || b.EndsWith("Editar") || b.EndsWith("Excluir"));
        n.Capturar("tarefas-usuario");
    });

    private Task<string> TokenGestor() => api.EntrarAsync(Configuracao.EmailGestor, Configuracao.SenhaGestor);

    // Executa o cenário e, em caso de falha, salva uma captura de tela antes de repassar o erro.
    private async Task Cenario(Func<Navegador, Task> passos, [CallerMemberName] string nome = "")
    {
        using var navegador = new Navegador(saida, nome);
        try
        {
            await passos(navegador);
        }
        catch
        {
            navegador.Capturar("FALHA");
            throw;
        }
    }

    public void Dispose() => api.Dispose();
}
