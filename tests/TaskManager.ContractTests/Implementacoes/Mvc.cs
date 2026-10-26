using TaskManager.ContractTests.Cenarios;
using TaskManager.ContractTests.Infraestrutura;
using Xunit;

// Cada classe abaixo executa os cenários comuns contra a API MVC.
// O código dos cenários é o mesmo da CleanArch; só a hospedagem muda.
namespace TaskManager.ContractTests.Mvc;

[Collection(ColecaoMvc.Nome)]
public sealed class CriacaoDeTarefaMvc(MvcApiFixture api) : CriacaoDeTarefa(api);

[Collection(ColecaoMvc.Nome)]
public sealed class TransicoesDeStatusMvc(MvcApiFixture api) : TransicoesDeStatus(api);

[Collection(ColecaoMvc.Nome)]
public sealed class PermissoesMvc(MvcApiFixture api) : Permissoes(api);

[Collection(ColecaoMvc.Nome)]
public sealed class AutenticacaoMvc(MvcApiFixture api) : Autenticacao(api);

[Collection(ColecaoMvc.Nome)]
public sealed class ExclusaoDeProjetoMvc(MvcApiFixture api) : ExclusaoDeProjeto(api);

[Collection(ColecaoMvc.Nome)]
public sealed class TarefaConcluidaEExclusaoMvc(MvcApiFixture api) : TarefaConcluidaEExclusao(api);

[Collection(ColecaoMvc.Nome)]
public sealed class ResponsaveisEContasMvc(MvcApiFixture api) : ResponsaveisEContas(api);

[Collection(ColecaoMvc.Nome)]
public sealed class CatalogoDeStatusMvc(MvcApiFixture api) : CatalogoDeStatus(api);

[Collection(ColecaoMvc.Nome)]
public sealed class ValidacaoDeEntradaMvc(MvcApiFixture api) : ValidacaoDeEntrada(api);

[Collection(ColecaoMvc.Nome)]
public sealed class ConcorrenciaMvc(MvcApiFixture api) : Concorrencia(api);

[Collection(ColecaoMvc.Nome)]
public sealed class CrudCompletoMvc(MvcApiFixture api) : CrudCompleto(api);
