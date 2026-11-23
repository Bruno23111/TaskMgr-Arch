using TaskManager.CleanArch.Application.Common;
using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.UnitTests.Application.Falsos;

namespace TaskManager.CleanArch.UnitTests.Application;

/// <summary>
/// Estado inicial de cada teste da Application: os três status do seed e um Gestor autenticado,
/// como no banco recém-criado da API. Tudo em memória; nenhum teste toca em banco.
/// </summary>
internal sealed class Ambiente
{
    public const string Senha = "SenhaUsuario123!";

    public Ambiente()
    {
        Status = new StatusEmMemoria();
        Tarefas = new TarefasEmMemoria();
        Projetos = new ProjetosEmMemoria(Tarefas, Status);

        Pendente = AdicionarStatus("PENDENTE", CategoriaStatus.Pendente, protegido: true);
        EmAndamento = AdicionarStatus("EM_ANDAMENTO", CategoriaStatus.EmAndamento, protegido: true);
        Concluida = AdicionarStatus("CONCLUIDA", CategoriaStatus.Concluida, protegido: true);

        (PerfilDoGestor, Gestor) = NovaConta(PapeisSistema.Gestor);
    }

    public StatusEmMemoria Status { get; }

    public TarefasEmMemoria Tarefas { get; }

    public ProjetosEmMemoria Projetos { get; }

    public ResponsaveisEmMemoria Responsaveis { get; } = new();

    public ContasEmMemoria Contas { get; } = new();

    public UnidadeDeTrabalhoFalsa UnidadeDeTrabalho { get; } = new();

    public RelogioFixo Relogio { get; } = new();

    public DespachanteFalso Despachante { get; } = new();

    public StatusTarefa Pendente { get; }

    public StatusTarefa EmAndamento { get; }

    public StatusTarefa Concluida { get; }

    public Responsavel PerfilDoGestor { get; }

    public UsuarioFalso Gestor { get; }

    public StatusTarefa AdicionarStatus(string codigo, CategoriaStatus categoria, bool protegido = false)
    {
        var status = StatusTarefa.Criar(codigo, codigo, categoria, protegido);
        Status.Adicionar(status);
        return status;
    }

    public StatusTarefa DoSeed(CategoriaStatus categoria) => categoria switch
    {
        CategoriaStatus.Pendente => Pendente,
        CategoriaStatus.EmAndamento => EmAndamento,
        _ => Concluida,
    };

    public Projeto NovoProjeto(string nome = "Projeto")
    {
        var projeto = Projeto.Criar(nome, null);
        Projetos.Adicionar(projeto);
        return projeto;
    }

    /// <summary>Conta de autenticação + perfil de responsável, e o usuário autenticado correspondente.</summary>
    public (Responsavel Perfil, UsuarioFalso Usuario) NovaConta(string papel = PapeisSistema.Usuario, bool ativo = true)
    {
        var usuarioId = Contas.Adicionar($"{Guid.NewGuid():N}@teste.local", Senha, papel);
        var perfil = Responsavel.Criar(usuarioId, $"{papel} {Responsaveis.Itens.Count + 1}");
        Responsaveis.Adicionar(perfil);
        if (!ativo)
            Desativar(perfil);

        return (perfil, new UsuarioFalso(usuarioId, Autenticado: true, Gestor: papel == PapeisSistema.Gestor));
    }

    public void Desativar(Responsavel perfil)
    {
        perfil.Desativar(possuiTarefaVisivelNaoConcluida: false);
        Contas.DesativarContaAsync(perfil.UsuarioId).GetAwaiter().GetResult();
    }

    /// <summary>Tarefa levada até a categoria pedida pelo caminho permitido da matriz.</summary>
    public Tarefa NovaTarefa(CategoriaStatus categoria = CategoriaStatus.Pendente, Responsavel? responsavel = null, Projeto? projeto = null)
    {
        var tarefa = Tarefa.Criar((projeto ?? NovoProjeto()).Id, "Tarefa", null, Pendente.Id);
        if (responsavel is not null)
            tarefa.AtribuirResponsavel(responsavel.Id, CategoriaStatus.Pendente, responsavelAtivo: true);

        if (categoria is CategoriaStatus.EmAndamento or CategoriaStatus.Concluida)
            tarefa.AlterarStatus(EmAndamento.Id, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, true, true, Relogio.DataHoraAgoraUTC);

        if (categoria is CategoriaStatus.Concluida)
            tarefa.AlterarStatus(Concluida.Id, CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, true, true, Relogio.DataHoraAgoraUTC);

        tarefa.LimparDomainEvents();
        Tarefas.Adicionar(tarefa);
        return tarefa;
    }
}
