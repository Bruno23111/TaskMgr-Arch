using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Domain;

public sealed class StatusTarefaTests
{
    [Fact]
    [Trait("CA", "CA34")]
    public void Criar_NormalizaCodigoEmMaiusculasEIniciaDesprotegidoNaVersao1()
    {
        var status = StatusTarefa.Criar("  bloqueada ", " Bloqueada ", CategoriaStatus.Pendente);

        Assert.Equal("BLOQUEADA", status.Codigo);
        Assert.Equal("Bloqueada", status.Nome);
        Assert.Equal(CategoriaStatus.Pendente, status.Categoria);
        Assert.False(status.Protegido);
        Assert.Equal(1, status.Versao);
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("", "Nome")]
    [InlineData("codigo-longo-demais", "Nome")]
    [InlineData("CODIGO", "")]
    [InlineData("CODIGO", "nome-longo-demais")]
    public void Criar_ComDadosInvalidos_LancaArgumentException(string codigo, string nome)
    {
        codigo = codigo == "codigo-longo-demais" ? new string('A', 21) : codigo;
        nome = nome == "nome-longo-demais" ? new string('a', 61) : nome;

        Assert.Throws<ArgumentException>(() => StatusTarefa.Criar(codigo, nome, CategoriaStatus.Pendente));
    }

    [Fact]
    [Trait("CA", "CA24")]
    public void Renomear_StatusProtegido_TrocaSoONome()
    {
        var status = StatusTarefa.Criar("PENDENTE", "Pendente", CategoriaStatus.Pendente, protegido: true);

        status.Renomear("A fazer");

        Assert.Equal(("A fazer", "PENDENTE", CategoriaStatus.Pendente, true, 2),
            (status.Nome, status.Codigo, status.Categoria, status.Protegido, status.Versao));
    }

    [Theory]
    [Trait("CA", "CA23")]
    [InlineData(false)]
    [InlineData(true)]
    public void StatusProtegido_NuncaPodeSerExcluido_ViolaRegistroProtegido(bool possuiReferencias)
    {
        var status = StatusTarefa.Criar("CONCLUIDA", "Concluída", CategoriaStatus.Concluida, protegido: true);

        // A proteção tem prioridade sobre o uso: o código é o mesmo com ou sem tarefas.
        Regras.DeveViolar("REGISTRO_PROTEGIDO", () => status.GarantirPodeSerExcluido(possuiReferencias));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public void StatusAdicionalReferenciado_NaoPodeSerExcluido_ViolaRegistroEmUso()
    {
        var status = StatusTarefa.Criar("BLOQUEADA", "Bloqueada", CategoriaStatus.Pendente);

        Regras.DeveViolar("REGISTRO_EM_USO", () => status.GarantirPodeSerExcluido(possuiReferencias: true));
    }

    [Fact]
    [Trait("CA", "CA23")]
    public void StatusAdicionalLivre_PodeSerExcluido()
    {
        var status = StatusTarefa.Criar("BLOQUEADA", "Bloqueada", CategoriaStatus.Pendente);

        status.GarantirPodeSerExcluido(possuiReferencias: false);
    }
}
