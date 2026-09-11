using TaskManager.CleanArch.Domain.Common;

namespace TaskManager.CleanArch.Domain.Entities;

public sealed class Responsavel
{
    private Responsavel()
    {
    }

    private Responsavel(Guid id, string usuarioId, string nome)
    {
        Id = id;
        UsuarioId = ValidarUsuarioId(usuarioId);
        AtualizarNome(nome, incrementaVersao: false);
        Ativo = true;
        Versao = 1;
    }

    public Guid Id { get; private set; }
    public string UsuarioId { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public bool Ativo { get; private set; }
    public int Versao { get; private set; }

    public static Responsavel Criar(string usuarioId, string nome) => new(Guid.NewGuid(), usuarioId, nome);

    public void AtualizarNome(string nome) => AtualizarNome(nome, incrementaVersao: true);

    public void Desativar(bool possuiTarefaVisivelNaoConcluida)
    {
        if (possuiTarefaVisivelNaoConcluida)
            throw new DomainRuleViolationException("REGISTRO_EM_USO", "Responsável possui tarefa visível pendente ou em andamento.");
        if (!Ativo)
            return;
        Ativo = false;
        Versao++;
    }

    public void GarantirPodeSerExcluido(bool possuiReferencias)
    {
        if (possuiReferencias)
            throw new DomainRuleViolationException("REGISTRO_EM_USO", "Responsável com tarefas vinculadas não pode ser excluído.");
    }

    private void AtualizarNome(string nome, bool incrementaVersao)
    {
        Projeto.GarantirTexto(nome, nameof(nome), 100);
        Nome = nome.Trim();
        if (incrementaVersao)
            Versao++;
    }

    private static string ValidarUsuarioId(string usuarioId)
    {
        Projeto.GarantirTexto(usuarioId, nameof(usuarioId), 450);
        return usuarioId.Trim();
    }
}
