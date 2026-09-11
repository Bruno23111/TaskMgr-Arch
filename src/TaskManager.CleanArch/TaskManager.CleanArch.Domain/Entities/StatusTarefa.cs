using TaskManager.CleanArch.Domain.Common;
using TaskManager.CleanArch.Domain.Enums;

namespace TaskManager.CleanArch.Domain.Entities;

public sealed class StatusTarefa
{
    private StatusTarefa()
    {
    }

    private StatusTarefa(Guid id, string codigo, string nome, CategoriaStatus categoria, bool protegido)
    {
        Id = id;
        Codigo = ValidarCodigo(codigo);
        Categoria = categoria;
        Protegido = protegido;
        Renomear(nome, incrementaVersao: false);
        Versao = 1;
    }

    public Guid Id { get; private set; }
    public string Codigo { get; private set; } = null!;
    public string Nome { get; private set; } = null!;
    public CategoriaStatus Categoria { get; private set; }
    public bool Protegido { get; private set; }
    public int Versao { get; private set; }

    public static StatusTarefa Criar(string codigo, string nome, CategoriaStatus categoria, bool protegido = false) =>
        new(Guid.NewGuid(), codigo, nome, categoria, protegido);

    public void Renomear(string nome) => Renomear(nome, incrementaVersao: true);

    public void GarantirPodeSerExcluido(bool possuiReferencias)
    {
        if (Protegido)
            throw new DomainRuleViolationException("REGISTRO_PROTEGIDO", "Status protegidos não podem ser excluídos.");
        if (possuiReferencias)
            throw new DomainRuleViolationException("REGISTRO_EM_USO", "Status com tarefas vinculadas não pode ser excluído.");
    }

    private void Renomear(string nome, bool incrementaVersao)
    {
        Projeto.GarantirTexto(nome, nameof(nome), 60);
        Nome = nome.Trim();
        if (incrementaVersao)
            Versao++;
    }

    private static string ValidarCodigo(string codigo)
    {
        Projeto.GarantirTexto(codigo, nameof(codigo), 20);
        return codigo.Trim().ToUpperInvariant();
    }
}
