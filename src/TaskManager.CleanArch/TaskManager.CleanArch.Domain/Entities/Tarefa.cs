using TaskManager.CleanArch.Domain.Common;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.Domain.Events;

namespace TaskManager.CleanArch.Domain.Entities;

public sealed class Tarefa : AggregateRoot
{
    private Tarefa()
    {
    }

    private Tarefa(Guid id, Guid projetoId, string titulo, string? descricao, Guid statusId)
    {
        if (projetoId == Guid.Empty)
            throw new ArgumentException("O projeto é obrigatório.", nameof(projetoId));
        if (statusId == Guid.Empty)
            throw new ArgumentException("O status é obrigatório.", nameof(statusId));

        Id = id;
        ProjetoId = projetoId;
        StatusId = statusId;
        AtualizarConteudo(titulo, descricao, incrementaVersao: false);
        Versao = 1;
    }

    public Guid Id { get; private set; }
    public Guid ProjetoId { get; private set; }
    public Guid StatusId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public string Titulo { get; private set; } = null!;
    public string? Descricao { get; private set; }
    public DateTimeOffset? ConcluidaEm { get; private set; }
    public DateTimeOffset? ExcluidoEm { get; private set; }
    public int Versao { get; private set; }

    public static Tarefa Criar(Guid projetoId, string titulo, string? descricao, Guid statusPendenteId) =>
        new(Guid.NewGuid(), projetoId, titulo, descricao, statusPendenteId);

    public void AtualizarConteudo(string titulo, string? descricao, CategoriaStatus categoriaAtual)
    {
        GarantirAtiva();
        GarantirNaoConcluida(categoriaAtual);
        AtualizarConteudo(titulo, descricao, incrementaVersao: true);
    }

    public void AtribuirResponsavel(Guid responsavelId, CategoriaStatus categoriaAtual, bool responsavelAtivo)
    {
        GarantirAtiva();
        GarantirNaoConcluida(categoriaAtual);
        if (responsavelId == Guid.Empty)
            throw new ArgumentException("O responsável é obrigatório.", nameof(responsavelId));
        if (!responsavelAtivo)
            throw new DomainRuleViolationException("RESPONSAVEL_INATIVO", "Não é possível atribuir um responsável inativo.");
        if (ResponsavelId == responsavelId)
            return;
        ResponsavelId = responsavelId;
        Versao++;
    }

    public void RemoverResponsavel(CategoriaStatus categoriaAtual)
    {
        GarantirAtiva();
        GarantirNaoConcluida(categoriaAtual);
        if (ResponsavelId is null)
            return;
        ResponsavelId = null;
        Versao++;
    }

    public void AlterarStatus(
        Guid novoStatusId,
        CategoriaStatus categoriaAtual,
        CategoriaStatus novaCategoria,
        bool responsavelAtivo,
        bool solicitanteEhGestor,
        DateTimeOffset agoraUtc)
    {
        GarantirAtiva();
        if (novoStatusId == Guid.Empty)
            throw new ArgumentException("O status é obrigatório.", nameof(novoStatusId));

        if (StatusId == novoStatusId)
            return;

        ValidarTransicao(categoriaAtual, novaCategoria, solicitanteEhGestor);
        if (novaCategoria == CategoriaStatus.Concluida && !responsavelAtivo)
            throw new DomainRuleViolationException("RESPONSAVEL_OBRIGATORIO", "Uma tarefa só pode ser concluída com responsável ativo.");

        var statusAnteriorId = StatusId;
        StatusId = novoStatusId;
        if (categoriaAtual != CategoriaStatus.Concluida && novaCategoria == CategoriaStatus.Concluida)
            ConcluidaEm = Projeto.GarantirUtc(agoraUtc);
        else if (categoriaAtual == CategoriaStatus.Concluida && novaCategoria == CategoriaStatus.Pendente)
            ConcluidaEm = null;
        Versao++;
        AdicionarDomainEvent(new TarefaStatusAlteradoDomainEvent(
            Id,
            ProjetoId,
            statusAnteriorId,
            novoStatusId,
            categoriaAtual,
            novaCategoria,
            Projeto.GarantirUtc(agoraUtc)));
    }

    public void Excluir(CategoriaStatus categoriaAtual, DateTimeOffset agoraUtc)
    {
        GarantirAtiva();
        GarantirNaoConcluida(categoriaAtual);
        ExcluidoEm = Projeto.GarantirUtc(agoraUtc);
        Versao++;
    }

    private void AtualizarConteudo(string titulo, string? descricao, bool incrementaVersao)
    {
        Projeto.GarantirTexto(titulo, nameof(titulo), 100);
        Projeto.GarantirTamanho(descricao, nameof(descricao), 255);
        Titulo = titulo.Trim();
        Descricao = Projeto.NormalizarOpcional(descricao);
        if (incrementaVersao)
            Versao++;
    }

    private static void ValidarTransicao(CategoriaStatus atual, CategoriaStatus destino, bool solicitanteEhGestor)
    {
        var permitida = (atual, destino) switch
        {
            (CategoriaStatus.Pendente, CategoriaStatus.Pendente or CategoriaStatus.EmAndamento) => true,
            (CategoriaStatus.EmAndamento, _) => true,
            (CategoriaStatus.Concluida, CategoriaStatus.Concluida) => true,
            (CategoriaStatus.Concluida, CategoriaStatus.Pendente) when solicitanteEhGestor => true,
            _ => false
        };

        if (!permitida)
            throw new DomainRuleViolationException("TRANSICAO_INVALIDA", "A transição de status não é permitida.");
    }

    private static void GarantirNaoConcluida(CategoriaStatus categoriaAtual)
    {
        if (categoriaAtual == CategoriaStatus.Concluida)
            throw new DomainRuleViolationException("TAREFA_CONCLUIDA", "A tarefa concluída deve ser reaberta antes de ser alterada.");
    }

    private void GarantirAtiva()
    {
        if (ExcluidoEm is not null)
            throw new DomainRuleViolationException("RECURSO_EXCLUIDO", "A tarefa foi excluída.");
    }
}
