using TaskManager.CleanArch.Domain.Entities;
using Xunit;
using static TaskManager.CleanArch.UnitTests.Domain.CenariosDeTarefa;

namespace TaskManager.CleanArch.UnitTests.Domain;

public sealed class ProjetoTests
{
    [Fact]
    [Trait("CA", "CA34")]
    public void Criar_NormalizaOsTextosEIniciaNaVersao1()
    {
        var projeto = Projeto.Criar("  Site institucional ", "   ");

        Assert.NotEqual(Guid.Empty, projeto.Id);
        Assert.Equal("Site institucional", projeto.Nome);
        Assert.Null(projeto.Descricao);
        Assert.Equal(1, projeto.Versao);
        Assert.Null(projeto.ExcluidoEm);
    }

    [Theory]
    [Trait("CA", "CA29")]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("nome-longo-demais", null)]
    [InlineData("Projeto", "descricao-longa-demais")]
    public void Criar_ComDadosInvalidos_LancaArgumentException(string nome, string? descricao)
    {
        nome = nome == "nome-longo-demais" ? new string('a', 101) : nome;
        descricao = descricao == "descricao-longa-demais" ? new string('a', 256) : descricao;

        Assert.Throws<ArgumentException>(() => Projeto.Criar(nome, descricao));
    }

    [Fact]
    public void AtualizarDetalhes_TrocaOsDadosEIncrementaAVersao()
    {
        var projeto = Projeto.Criar("Site", "Antigo");

        projeto.AtualizarDetalhes("Site 2.0", null);

        Assert.Equal(("Site 2.0", (string?)null, 2), (projeto.Nome, projeto.Descricao, projeto.Versao));
    }

    [Fact]
    [Trait("CA", "CA16")]
    public void Excluir_RegistraExclusaoLogicaEIncrementaAVersao()
    {
        var projeto = Projeto.Criar("Site", null);

        projeto.Excluir(Agora);

        Assert.Equal(Agora, projeto.ExcluidoEm);
        Assert.Equal(2, projeto.Versao);
    }

    [Fact]
    public void Excluir_ComDataForaDeUtc_LancaArgumentException()
    {
        var projeto = Projeto.Criar("Site", null);

        Assert.Throws<ArgumentException>(() => projeto.Excluir(new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.FromHours(-3))));
        Assert.Null(projeto.ExcluidoEm);
    }

    [Theory]
    [Trait("CA", "CA30")]
    [InlineData("atualizar")]
    [InlineData("excluir")]
    public void ProjetoExcluido_NaoAceitaAlteracao_ViolaRecursoExcluido(string operacao)
    {
        var projeto = Projeto.Criar("Site", null);
        projeto.Excluir(Agora);
        Action acao = operacao == "atualizar"
            ? () => projeto.AtualizarDetalhes("Outro nome", null)
            : () => projeto.Excluir(Agora.AddMinutes(1));

        Regras.DeveViolar("RECURSO_EXCLUIDO", acao);

        Assert.Equal(("Site", Agora, 2), (projeto.Nome, projeto.ExcluidoEm!.Value, projeto.Versao));
    }
}
