using TaskManager.CleanArch.Domain.Entities;
using Xunit;

namespace TaskManager.CleanArch.UnitTests.Domain;

public sealed class ResponsavelTests
{
    [Fact]
    [Trait("CA", "CA34")]
    public void Criar_IniciaAtivoNaVersao1()
    {
        var responsavel = Responsavel.Criar(" usuario-1 ", "  Ana ");

        Assert.Equal(("usuario-1", "Ana", true, 1),
            (responsavel.UsuarioId, responsavel.Nome, responsavel.Ativo, responsavel.Versao));
    }

    [Theory]
    [InlineData("", "Ana")]
    [InlineData("usuario-1", "")]
    [InlineData("usuario-1", "nome-longo-demais")]
    public void Criar_ComDadosInvalidos_LancaArgumentException(string usuarioId, string nome)
    {
        nome = nome == "nome-longo-demais" ? new string('a', 101) : nome;

        Assert.Throws<ArgumentException>(() => Responsavel.Criar(usuarioId, nome));
    }

    [Fact]
    public void AtualizarNome_IncrementaAVersao()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");

        responsavel.AtualizarNome("Ana Souza");

        Assert.Equal(("Ana Souza", 2), (responsavel.Nome, responsavel.Versao));
    }

    [Fact]
    [Trait("CA", "CA21")]
    public void Desativar_ComTarefaVisivelNaoConcluida_ViolaRegistroEmUso()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");

        Regras.DeveViolar("REGISTRO_EM_USO", () => responsavel.Desativar(possuiTarefaVisivelNaoConcluida: true));

        Assert.True(responsavel.Ativo);
        Assert.Equal(1, responsavel.Versao);
    }

    [Fact]
    [Trait("CA", "CA22")]
    public void Desativar_SemTarefaPendente_InativaEIncrementaAVersao()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");

        responsavel.Desativar(possuiTarefaVisivelNaoConcluida: false);

        Assert.Equal((false, 2), (responsavel.Ativo, responsavel.Versao));
    }

    [Fact]
    public void Desativar_ResponsavelJaInativo_EhIdempotente()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");
        responsavel.Desativar(possuiTarefaVisivelNaoConcluida: false);

        responsavel.Desativar(possuiTarefaVisivelNaoConcluida: false);

        Assert.Equal((false, 2), (responsavel.Ativo, responsavel.Versao));
    }

    [Fact]
    [Trait("CA", "CA19")]
    public void Excluir_ComTarefasVinculadas_ViolaRegistroEmUso()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");

        Regras.DeveViolar("REGISTRO_EM_USO", () => responsavel.GarantirPodeSerExcluido(possuiReferencias: true));
    }

    [Fact]
    [Trait("CA", "CA20")]
    public void Excluir_SemTarefasVinculadas_EhPermitido()
    {
        var responsavel = Responsavel.Criar("usuario-1", "Ana");

        responsavel.GarantirPodeSerExcluido(possuiReferencias: false);
    }
}
