using TaskManager.ContractTests.Cenarios;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

// Cada classe abaixo executa os cenários comuns contra a API Clean Architecture.
// O código dos cenários é o mesmo do MVC; só a hospedagem muda.
namespace TaskManager.ContractTests.CleanArch;

[Collection(ColecaoCleanArch.Nome)]
public sealed class CriacaoDeTarefaCleanArch(CleanArchApiFixture api) : CriacaoDeTarefa(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class TransicoesDeStatusCleanArch(CleanArchApiFixture api) : TransicoesDeStatus(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class PermissoesCleanArch(CleanArchApiFixture api) : Permissoes(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class AutenticacaoCleanArch(CleanArchApiFixture api) : Autenticacao(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class ExclusaoDeProjetoCleanArch(CleanArchApiFixture api) : ExclusaoDeProjeto(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class TarefaConcluidaEExclusaoCleanArch(CleanArchApiFixture api) : TarefaConcluidaEExclusao(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class ResponsaveisEContasCleanArch(CleanArchApiFixture api) : ResponsaveisEContas(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class CatalogoDeStatusCleanArch(CleanArchApiFixture api) : CatalogoDeStatus(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class ValidacaoDeEntradaCleanArch(CleanArchApiFixture api) : ValidacaoDeEntrada(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class ConcorrenciaCleanArch(CleanArchApiFixture api) : Concorrencia(api);

[Collection(ColecaoCleanArch.Nome)]
public sealed class CrudCompletoCleanArch(CleanArchApiFixture api) : CrudCompleto(api);
