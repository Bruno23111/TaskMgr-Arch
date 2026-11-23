using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TaskManager.MVC.API.Data;
using TaskManager.MVC.API.Identity;
using TaskManager.MVC.API.Models;

namespace TaskManager.MVC.UnitTests.Infraestrutura;

/// <summary>
/// DbContext real da API sobre o provedor EF Core InMemory, com os três status do seed (HasData).
/// O provedor não tem transações nem SQL: operações que abrem transação ou usam SELECT ... FOR UPDATE
/// só podem ser verificadas até o ponto anterior à transação.
/// </summary>
internal sealed class BancoEmMemoria : IDisposable
{
    public static readonly Guid PendenteId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    public static readonly Guid EmAndamentoId = Guid.Parse("00000000-0000-0000-0000-000000000002");
    public static readonly Guid ConcluidaId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private readonly DbContextOptions<TaskManagerMvcDbContext> _opcoes =
        new DbContextOptionsBuilder<TaskManagerMvcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public BancoEmMemoria()
    {
        Contexto = new TaskManagerMvcDbContext(_opcoes);
        Contexto.Database.EnsureCreated();
    }

    /// <summary>Contexto usado pelos Services sob teste.</summary>
    public TaskManagerMvcDbContext Contexto { get; }

    /// <summary>Contexto novo, sem rastreamento compartilhado, para conferir o que foi de fato gravado.</summary>
    public TaskManagerMvcDbContext Leitura() => new(_opcoes);

    public static Guid StatusDe(CategoriaStatus categoria) => categoria switch
    {
        CategoriaStatus.Pendente => PendenteId,
        CategoriaStatus.EmAndamento => EmAndamentoId,
        _ => ConcluidaId,
    };

    public Projeto NovoProjeto(string nome = "Projeto")
    {
        var projeto = new Projeto { Nome = nome };
        Contexto.Projetos.Add(projeto);
        Contexto.SaveChanges();
        return projeto;
    }

    /// <summary>Conta de autenticação + perfil, como o AccountService cria.</summary>
    public Responsavel NovoResponsavel(string usuarioId, bool ativo = true)
    {
        Contexto.Users.Add(new UsuarioIdentity { Id = usuarioId, UserName = usuarioId, Email = $"{usuarioId}@teste.local", Ativo = ativo });
        var responsavel = new Responsavel { UsuarioId = usuarioId, Nome = usuarioId, Ativo = ativo };
        Contexto.Responsaveis.Add(responsavel);
        Contexto.SaveChanges();
        return responsavel;
    }

    /// <summary>Tarefa gravada diretamente na categoria pedida (o caminho pela API exige transação).</summary>
    public Tarefa NovaTarefa(CategoriaStatus categoria = CategoriaStatus.Pendente, Responsavel? responsavel = null, Projeto? projeto = null)
    {
        var tarefa = new Tarefa
        {
            ProjetoId = (projeto ?? NovoProjeto()).Id,
            StatusId = StatusDe(categoria),
            ResponsavelId = responsavel?.Id,
            Titulo = "Tarefa",
            ConcluidaEm = categoria == CategoriaStatus.Concluida ? DateTimeOffset.UtcNow : null,
        };
        Contexto.Tarefas.Add(tarefa);
        Contexto.SaveChanges();
        return tarefa;
    }

    public void Dispose() => Contexto.Dispose();
}

internal static class Usuarios
{
    public const string GestorId = "gestor-inicial";

    public static CurrentUser Gestor() => Com(GestorId, "Gestor");

    public static CurrentUser Comum(string usuarioId) => Com(usuarioId, "Usuario");

    private static CurrentUser Com(string usuarioId, string papel)
    {
        var identidade = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, usuarioId), new Claim(ClaimTypes.Role, papel)],
            authenticationType: "Teste");
        var acessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identidade) } };
        return new CurrentUser(acessor);
    }
}
