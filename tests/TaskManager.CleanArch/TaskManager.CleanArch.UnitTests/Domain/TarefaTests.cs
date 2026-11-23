using TaskManager.CleanArch.Domain.Entities;
using TaskManager.CleanArch.Domain.Enums;
using TaskManager.CleanArch.Domain.Events;
using Xunit;
using static TaskManager.CleanArch.UnitTests.Domain.CenariosDeTarefa;

namespace TaskManager.CleanArch.UnitTests.Domain;

public sealed class TarefaTests
{
    [Fact]
    [Trait("CA", "CA01")]
    public void Criar_IniciaPendenteNaVersao1SemResponsavelESemConclusao()
    {
        var tarefa = Nova();

        Assert.Equal(ProjetoId, tarefa.ProjetoId);
        Assert.Equal(StatusDe(CategoriaStatus.Pendente), tarefa.StatusId);
        Assert.Equal(1, tarefa.Versao);
        Assert.Null(tarefa.ResponsavelId);
        Assert.Null(tarefa.ConcluidaEm);
        Assert.Null(tarefa.ExcluidoEm);
        Assert.Empty(tarefa.DomainEvents);
    }

    [Fact]
    public void Criar_NormalizaTituloEDescricaoEmBranco()
    {
        var tarefa = Tarefa.Criar(ProjetoId, "  Escrever testes  ", "   ", StatusDe(CategoriaStatus.Pendente));

        Assert.Equal("Escrever testes", tarefa.Titulo);
        Assert.Null(tarefa.Descricao);
    }

    [Theory]
    [InlineData("projeto-vazio")]
    [InlineData("status-vazio")]
    [InlineData("titulo-vazio")]
    [InlineData("titulo-em-branco")]
    [InlineData("titulo-longo-demais")]
    [InlineData("descricao-longa-demais")]
    public void Criar_ComDadosInvalidos_LancaArgumentException(string caso)
    {
        var projeto = caso == "projeto-vazio" ? Guid.Empty : ProjetoId;
        var status = caso == "status-vazio" ? Guid.Empty : StatusDe(CategoriaStatus.Pendente);
        var titulo = caso switch
        {
            "titulo-vazio" => "",
            "titulo-em-branco" => "   ",
            "titulo-longo-demais" => new string('a', 101),
            _ => "Título",
        };
        var descricao = caso == "descricao-longa-demais" ? new string('a', 256) : null;

        Assert.ThrowsAny<ArgumentException>(() => Tarefa.Criar(projeto, titulo, descricao, status));
    }

    [Fact]
    public void Criar_AceitaTituloEDescricaoNoLimite()
    {
        var tarefa = Tarefa.Criar(ProjetoId, new string('a', 100), new string('b', 255), StatusDe(CategoriaStatus.Pendente));

        Assert.Equal(100, tarefa.Titulo.Length);
        Assert.Equal(255, tarefa.Descricao!.Length);
    }

    [Fact]
    [Trait("CA", "CA05")]
    [Trait("RN", "RN05")]
    public void Concluir_ComResponsavelAtivo_PreencheConcluidaEmComADataInformada()
    {
        var tarefa = Em(CategoriaStatus.EmAndamento);

        Mover(tarefa, CategoriaStatus.EmAndamento, CategoriaStatus.Concluida, Agora.AddHours(1));

        Assert.Equal(StatusDe(CategoriaStatus.Concluida), tarefa.StatusId);
        Assert.Equal(Agora.AddHours(1), tarefa.ConcluidaEm);
        Assert.Equal(TimeSpan.Zero, tarefa.ConcluidaEm!.Value.Offset);
    }

    [Theory]
    [Trait("CA", "CA06")]
    [Trait("RN", "RN03")]
    [InlineData(false)]
    [InlineData(true)]
    public void Concluir_SemResponsavelAtivo_ViolaResponsavelObrigatorio(bool temResponsavelInativo)
    {
        var tarefa = Em(CategoriaStatus.EmAndamento, comResponsavel: temResponsavelInativo);
        var versao = tarefa.Versao;

        Regras.DeveViolar("RESPONSAVEL_OBRIGATORIO", () => tarefa.AlterarStatus(
            StatusDe(CategoriaStatus.Concluida),
            CategoriaStatus.EmAndamento,
            CategoriaStatus.Concluida,
            responsavelAtivo: false,
            solicitanteEhGestor: true,
            Agora));

        Assert.Equal(StatusDe(CategoriaStatus.EmAndamento), tarefa.StatusId);
        Assert.Null(tarefa.ConcluidaEm);
        Assert.Equal(versao, tarefa.Versao);
    }

    [Fact]
    [Trait("CA", "CA07")]
    [Trait("RN", "RN02")]
    public void PendenteSemResponsavelParaConcluida_ViolaAMatrizAntesDaExigenciaDeResponsavel()
    {
        var tarefa = Nova();

        Regras.DeveViolar("TRANSICAO_INVALIDA", () => tarefa.AlterarStatus(
            StatusDe(CategoriaStatus.Concluida),
            CategoriaStatus.Pendente,
            CategoriaStatus.Concluida,
            responsavelAtivo: false,
            solicitanteEhGestor: true,
            Agora));
    }

    [Fact]
    [Trait("CA", "CA08")]
    [Trait("RN", "RN04")]
    public void Reabrir_PeloGestor_LimpaAConclusaoEPreservaOResponsavel()
    {
        var tarefa = Em(CategoriaStatus.Concluida);

        Mover(tarefa, CategoriaStatus.Concluida, CategoriaStatus.Pendente);

        Assert.Equal(StatusDe(CategoriaStatus.Pendente), tarefa.StatusId);
        Assert.Null(tarefa.ConcluidaEm);
        Assert.Equal(ResponsavelId, tarefa.ResponsavelId);
    }

    [Fact]
    [Trait("RN", "RN05")]
    public void MudarEntreStatusConcluidos_PreservaADataDeConclusao()
    {
        var tarefa = Em(CategoriaStatus.Concluida);
        var concluidaEm = tarefa.ConcluidaEm;

        tarefa.AlterarStatus(Guid.NewGuid(), CategoriaStatus.Concluida, CategoriaStatus.Concluida,
            responsavelAtivo: true, solicitanteEhGestor: true, Agora.AddDays(3));

        Assert.Equal(concluidaEm, tarefa.ConcluidaEm);
    }

    [Fact]
    [Trait("CA", "CA04")]
    public void VoltarDeEmAndamentoParaPendente_MantemSemDataDeConclusao()
    {
        var tarefa = Em(CategoriaStatus.EmAndamento);

        Mover(tarefa, CategoriaStatus.EmAndamento, CategoriaStatus.Pendente);

        Assert.Equal(StatusDe(CategoriaStatus.Pendente), tarefa.StatusId);
        Assert.Null(tarefa.ConcluidaEm);
    }

    [Fact]
    [Trait("CA", "CA25")]
    public void MesmoStatus_EhIdempotente_SemVersaoNovaNemEvento()
    {
        var tarefa = Em(CategoriaStatus.EmAndamento);
        var versao = tarefa.Versao;

        // Mesmo sem responsável ativo: a repetição não reavalia regras de transição.
        tarefa.AlterarStatus(tarefa.StatusId, CategoriaStatus.EmAndamento, CategoriaStatus.EmAndamento,
            responsavelAtivo: false, solicitanteEhGestor: false, Agora);

        Assert.Equal(versao, tarefa.Versao);
        Assert.Empty(tarefa.DomainEvents);
    }

    [Fact]
    public void MudancaDeStatus_EmiteEventoDeDominioComOrigemEDestino()
    {
        var tarefa = Em(CategoriaStatus.Pendente);

        Mover(tarefa, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento);

        var evento = Assert.IsType<TarefaStatusAlteradoDomainEvent>(Assert.Single(tarefa.DomainEvents));
        Assert.Equal(
            new TarefaStatusAlteradoDomainEvent(
                tarefa.Id,
                ProjetoId,
                StatusDe(CategoriaStatus.Pendente),
                StatusDe(CategoriaStatus.EmAndamento),
                CategoriaStatus.Pendente,
                CategoriaStatus.EmAndamento,
                Agora),
            evento);
    }

    [Fact]
    public void LimparDomainEvents_DevolveOsEventosEEsvaziaALista()
    {
        var tarefa = Em(CategoriaStatus.Pendente);
        Mover(tarefa, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento);

        var eventos = tarefa.LimparDomainEvents();

        Assert.Single(eventos);
        Assert.Empty(tarefa.DomainEvents);
    }

    [Fact]
    public void MudancaDeStatus_ComDataForaDeUtc_LancaArgumentException()
    {
        var tarefa = Em(CategoriaStatus.EmAndamento);
        var horarioDeBrasilia = new DateTimeOffset(2026, 1, 15, 9, 0, 0, TimeSpan.FromHours(-3));

        Assert.Throws<ArgumentException>(() => tarefa.AlterarStatus(
            StatusDe(CategoriaStatus.Concluida),
            CategoriaStatus.EmAndamento,
            CategoriaStatus.Concluida,
            responsavelAtivo: true,
            solicitanteEhGestor: true,
            horarioDeBrasilia));
    }

    [Fact]
    public void MudancaDeStatus_ParaStatusVazio_LancaArgumentException()
    {
        var tarefa = Nova();

        Assert.Throws<ArgumentException>(() => tarefa.AlterarStatus(
            Guid.Empty, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento, true, true, Agora));
    }

    [Theory]
    [Trait("RN", "RN06")]
    [InlineData("editar-conteudo")]
    [InlineData("atribuir-responsavel")]
    [InlineData("remover-responsavel")]
    [InlineData("excluir")]
    public void TarefaConcluida_NaoAceitaAlteracao_ViolaTarefaConcluida(string operacao)
    {
        var tarefa = Em(CategoriaStatus.Concluida);
        var versao = tarefa.Versao;
        Action acao = operacao switch
        {
            "editar-conteudo" => () => tarefa.AtualizarConteudo("Novo título", null, CategoriaStatus.Concluida),
            "atribuir-responsavel" => () => tarefa.AtribuirResponsavel(Guid.NewGuid(), CategoriaStatus.Concluida, responsavelAtivo: true),
            "remover-responsavel" => () => tarefa.RemoverResponsavel(CategoriaStatus.Concluida),
            _ => () => tarefa.Excluir(CategoriaStatus.Concluida, Agora),
        };

        Regras.DeveViolar("TAREFA_CONCLUIDA", acao);

        Assert.Equal(versao, tarefa.Versao);
        Assert.Equal(ResponsavelId, tarefa.ResponsavelId);
        Assert.Null(tarefa.ExcluidoEm);
    }

    [Fact]
    [Trait("RN", "RN06")]
    public void AposReabrir_AEdicaoVoltaASerPermitida()
    {
        var tarefa = Em(CategoriaStatus.Concluida);
        Mover(tarefa, CategoriaStatus.Concluida, CategoriaStatus.Pendente);

        tarefa.AtualizarConteudo("Título revisado", null, CategoriaStatus.Pendente);

        Assert.Equal("Título revisado", tarefa.Titulo);
    }

    [Theory]
    [Trait("CA", "CA18")]
    [InlineData(CategoriaStatus.Pendente)]
    [InlineData(CategoriaStatus.EmAndamento)]
    public void Excluir_TarefaNaoConcluida_RegistraExclusaoLogica(CategoriaStatus categoria)
    {
        var tarefa = Em(categoria);
        var versao = tarefa.Versao;

        tarefa.Excluir(categoria, Agora);

        Assert.Equal(Agora, tarefa.ExcluidoEm);
        Assert.Equal(versao + 1, tarefa.Versao);
    }

    [Fact]
    [Trait("CA", "CA16")]
    public void ExcluirComProjeto_AceitaTarefaConcluida()
    {
        var tarefa = Em(CategoriaStatus.Concluida);

        tarefa.ExcluirComProjeto(Agora);

        Assert.Equal(Agora, tarefa.ExcluidoEm);
    }

    [Theory]
    [InlineData("editar-conteudo")]
    [InlineData("atribuir-responsavel")]
    [InlineData("remover-responsavel")]
    [InlineData("alterar-status")]
    [InlineData("excluir")]
    [InlineData("excluir-com-projeto")]
    public void TarefaExcluida_NaoAceitaNenhumaOperacao_ViolaRecursoExcluido(string operacao)
    {
        var tarefa = Em(CategoriaStatus.Pendente);
        tarefa.Excluir(CategoriaStatus.Pendente, Agora);
        Action acao = operacao switch
        {
            "editar-conteudo" => () => tarefa.AtualizarConteudo("Novo", null, CategoriaStatus.Pendente),
            "atribuir-responsavel" => () => tarefa.AtribuirResponsavel(Guid.NewGuid(), CategoriaStatus.Pendente, responsavelAtivo: true),
            "remover-responsavel" => () => tarefa.RemoverResponsavel(CategoriaStatus.Pendente),
            "alterar-status" => () => Mover(tarefa, CategoriaStatus.Pendente, CategoriaStatus.EmAndamento),
            "excluir" => () => tarefa.Excluir(CategoriaStatus.Pendente, Agora),
            _ => () => tarefa.ExcluirComProjeto(Agora),
        };

        Regras.DeveViolar("RECURSO_EXCLUIDO", acao);
    }

    [Fact]
    public void AtualizarConteudo_TrocaTituloEDescricaoEIncrementaAVersao()
    {
        var tarefa = Nova();

        tarefa.AtualizarConteudo("  Novo título ", " Nova descrição ", CategoriaStatus.Pendente);

        Assert.Equal("Novo título", tarefa.Titulo);
        Assert.Equal("Nova descrição", tarefa.Descricao);
        Assert.Equal(2, tarefa.Versao);
    }

    [Fact]
    [Trait("CA", "CA28")]
    public void Atribuir_ResponsavelInativo_ViolaResponsavelInativo()
    {
        var tarefa = Nova();

        Regras.DeveViolar("RESPONSAVEL_INATIVO", () =>
            tarefa.AtribuirResponsavel(ResponsavelId, CategoriaStatus.Pendente, responsavelAtivo: false));

        Assert.Null(tarefa.ResponsavelId);
        Assert.Equal(1, tarefa.Versao);
    }

    [Fact]
    public void Atribuir_ResponsavelVazio_LancaArgumentException()
    {
        var tarefa = Nova();

        Assert.Throws<ArgumentException>(() => tarefa.AtribuirResponsavel(Guid.Empty, CategoriaStatus.Pendente, true));
    }

    [Fact]
    [Trait("CA", "CA34")]
    public void AtribuirERemoverResponsavel_IncrementaAVersaoACadaPasso()
    {
        var tarefa = Nova();

        tarefa.AtribuirResponsavel(ResponsavelId, CategoriaStatus.Pendente, responsavelAtivo: true);
        Assert.Equal((ResponsavelId, 2), (tarefa.ResponsavelId!.Value, tarefa.Versao));

        tarefa.RemoverResponsavel(CategoriaStatus.Pendente);
        Assert.Equal(((Guid?)null, 3), (tarefa.ResponsavelId, tarefa.Versao));
    }

    [Fact]
    public void AtribuirOMesmoResponsavel_NaoGeraVersaoNova()
    {
        var tarefa = Em(CategoriaStatus.Pendente);
        var versao = tarefa.Versao;

        tarefa.AtribuirResponsavel(ResponsavelId, CategoriaStatus.Pendente, responsavelAtivo: true);

        Assert.Equal(versao, tarefa.Versao);
    }

    [Fact]
    public void RemoverResponsavel_DeTarefaSemResponsavel_NaoGeraVersaoNova()
    {
        var tarefa = Nova();

        tarefa.RemoverResponsavel(CategoriaStatus.Pendente);

        Assert.Equal(1, tarefa.Versao);
    }
}
