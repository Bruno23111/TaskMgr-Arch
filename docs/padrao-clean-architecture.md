# Padrão de implementação - Clean Architecture

Este documento define o padrão obrigatório para a API `TaskManager.CleanArch`. Ele deve ser consultado antes de criar ou alterar código nas camadas Domain, Application, Infrastructure e WebAPI.

O TCC compara uma implementação MVC com uma implementação Clean Architecture do mesmo gerenciador de tarefas. Portanto, a separação interna desta API não pode mudar o comportamento externo: rotas, contratos OpenAPI, regras de negócio, respostas HTTP, códigos de erro e critérios de aceitação devem permanecer equivalentes entre as duas arquiteturas.

Fontes normativas do projeto:

- [Requisitos funcionais](requisitos-funcionais.md): regras de negócio e critérios de aceitação CA01-CA34.
- [Contrato OpenAPI](openapi/taskmgr-v1.yaml): contrato HTTP comum às duas APIs.
- [Diagrama de componentes](diagramas/componentes-clean-architecture.md): visão arquitetural de referência.
- Artigo `ARTIGO_CleanArchitecture_vs_MVC_v3-revisado.pdf`: contexto científico da comparação de arquiteturas, manutenibilidade, complexidade e desenvolvimento assistido por IA.

## 1. Objetivo arquitetural

A Clean Architecture deve manter regras de negócio independentes de banco de dados, EF Core, ASP.NET Core, Identity, JWT e HTTP. Dependências de código sempre apontam para dentro.

```text
WebAPI -> Application -> Domain
WebAPI -> Infrastructure -> Application -> Domain
Infrastructure -> Domain
```

`WebAPI -> Infrastructure` é permitido somente no ponto de composição (`Program.cs`) para registrar dependências. Controllers não podem depender diretamente de Infrastructure.

## 2. Estrutura de projetos

```text
src/TaskManager.CleanArch/
├── TaskManager.CleanArch.Domain/
│   ├── Common/              # AggregateRoot, DomainRuleViolationException, contratos de eventos
│   ├── Entities/            # Projeto, Tarefa, Responsavel, StatusTarefa
│   ├── Enums/               # CategoriaStatus e demais conceitos estáveis do domínio
│   └── Events/              # Eventos de domínio
├── TaskManager.CleanArch.Application/
│   ├── Abstractions/        # Interfaces que a aplicação precisa
│   │   ├── Persistence/     # Repositórios e unidade de trabalho
│   │   ├── Identity/        # Autenticação, conta e usuário atual
│   │   └── Services/        # Relógio, token ou adaptadores externos, quando necessários
│   ├── Features/            # Casos de uso por recurso e operação
│   │   ├── Tarefas/
│   │   ├── Projetos/
│   │   ├── Responsaveis/
│   │   ├── Status/
│   │   └── Autenticacao/
│   ├── DTOs/                # Modelos de entrada/saída dos casos de uso, não modelos HTTP
│   └── Behaviors/           # Validação, transação e despacho de eventos via MediatR
├── TaskManager.CleanArch.Infrastructure/
│   ├── Persistence/         # DbContext, configurações EF Core e migrations
│   ├── Repositories/        # Implementações EF Core das interfaces da Application
│   ├── Identity/            # ASP.NET Core Identity e JWT
│   └── DependencyInjection/ # Registro de implementações
└── TaskManager.CleanArch.WebAPI/
    ├── Controllers/         # Adaptadores HTTP finos
    ├── Contracts/           # Requests e responses HTTP compatíveis com OpenAPI
    ├── Configuration/       # Problem Details, autenticação e composição da API
    └── Program.cs
```

Não criar referências de projeto no sentido contrário. Em especial, Domain nunca referencia Application, Infrastructure ou WebAPI; Application nunca referencia Infrastructure ou WebAPI.

## 3. Regras por camada

### Domain

É o centro do negócio e contém entidades ricas, value objects quando justificáveis, enums, invariantes e eventos de domínio.

- Entidades expõem métodos de intenção: `AlterarStatus`, `AtribuirResponsavel`, `Reabrir`, `Excluir` e `AtualizarConteudo`; evitar setters públicos.
- Regras que dependem apenas do estado da entidade ficam na própria entidade.
- `Tarefa` é o agregado que controla transição de status, conclusão, responsável e exclusão individual.
- O modelo usa `CategoriaStatus`, não nome exibido ou ID fixo, para avaliar a matriz de transições.
- Eventos de domínio descrevem fatos já ocorridos, por exemplo `TarefaStatusAlteradoDomainEvent`; não executam acesso a banco, HTTP ou serviços externos.
- Exceções de domínio devem representar regra violada e ter código estável. A conversão para HTTP acontece fora do Domain.
- Não incluir `DbContext`, atributos do EF Core, `HttpContext`, `ControllerBase`, tipos de Identity, JWT ou DTOs HTTP.

### Application

Coordena casos de uso. Esta camada carrega dados por abstrações, valida autorização contextual, chama métodos do Domain, persiste a alteração e devolve um resultado para a WebAPI.

- Um caso de uso deve ter uma responsabilidade clara, por exemplo `CriarTarefaCommand`, `AlterarStatusTarefaCommand` ou `ExcluirProjetoCommand`.
- Usar handlers MediatR para comandos e consultas. Não concentrar CRUD em um `TarefaService` genérico.
- Interfaces de repositório pertencem aqui porque a Application define o que precisa persistir: `ITarefaRepository`, `IProjetoRepository`, `IResponsavelRepository`, `IStatusRepository` e `IUnidadeDeTrabalho`.
- Interfaces para detalhes externos também pertencem aqui: `IUsuarioAtual`, `ITokenService`, `IContaService`, `IRelogio` e similares.
- DTOs de Application não são requests/responses HTTP. Eles descrevem o resultado do caso de uso e podem ser mapeados pela WebAPI.
- Autorização de recurso acontece aqui: o usuário só edita a tarefa atribuída a ele; Gestor executa operações globais; reabertura exige Gestor.
- A Application compara a versão recebida com a versão persistida e coordena operações atômicas.

### Infrastructure

Implementa adaptadores técnicos, sem conter regras de negócio.

- `TaskManagerCleanArchDbContext`, mapeamentos e migrations EF Core ficam em `Persistence`.
- Repositórios implementam as interfaces definidas na Application e conhecem PostgreSQL/Npgsql.
- Identity, hash de senha e emissão de JWT ficam em `Identity`.
- Consultas de integridade e bloqueios transacionais são implementados aqui, mas a decisão de negócio é feita por Domain/Application.
- Configurações EF devem preservar exclusão lógica, versão de concorrência, unicidade de código/e-mail e FKs sem cascata indevida.
- Nenhum repositório retorna `IQueryable` para Application ou WebAPI.

### WebAPI

É apenas a borda HTTP da aplicação.

- Controllers recebem request HTTP, chamam MediatR e mapeiam o resultado para response HTTP.
- Controllers não consultam `DbContext`, não aplicam regras de status e não fazem autorização de recurso manualmente.
- Requests/responses devem respeitar `docs/openapi/taskmgr-v1.yaml`.
- Falhas seguem `application/problem+json` com `code` estável, conforme requisitos funcionais.
- Autenticação JWT é configurada na borda; a decisão de autorização contextual permanece na Application.

## 4. Interfaces, services e repositórios

Interfaces são usadas para inverter dependências técnicas, não para duplicar cada classe.

| Necessidade | Contrato na Application | Implementação |
| --- | --- | --- |
| Persistir e consultar tarefas | `ITarefaRepository` | `EfTarefaRepository` na Infrastructure |
| Persistir e consultar projetos | `IProjetoRepository` | `EfProjetoRepository` na Infrastructure |
| Persistir e consultar status | `IStatusRepository` | `EfStatusRepository` na Infrastructure |
| Salvar uma operação atômica | `IUnidadeDeTrabalho` | EF Core/transaction na Infrastructure |
| Obter usuário, papel e estado atual | `IUsuarioAtual` | Adaptador de claims/Identity |
| Criar conta e emitir token | `IContaService`, `ITokenService` | Identity/JWT na Infrastructure |
| Tempo atual UTC | `IRelogio` | `SystemClock` na Infrastructure |

Não criar interface para uma entidade ou para uma classe puramente interna sem troca real de implementação. Em especial, não usar `ITarefaService` como fachada genérica para todos os casos de uso; prefira handlers pequenos orientados à ação.

## 5. Fluxo padrão de um caso de uso

```text
HTTP request
  -> Controller valida o formato HTTP
  -> Command/Query + Handler (Application)
  -> carrega agregados por interfaces
  -> valida permissão contextual e versão
  -> entidade aplica a regra
  -> evento de domínio é registrado
  -> unidade de trabalho persiste de forma atômica
  -> eventos são despachados após persistência bem-sucedida
  -> Controller mapeia resposta ou Problem Details
```

Ordem obrigatória de validação observável: autenticação, permissão global, corpo da requisição, existência do alvo, permissão sobre o alvo, versão e regras de negócio. Para mudança de status, validar a matriz antes da regra de responsável ativo.

## 6. Persistência e concorrência

- Projetos e tarefas usam exclusão lógica com `ExcluidoEm` UTC; consultas normais não retornam registros excluídos.
- A criação de tarefa, a transição de status e a exclusão de projeto devem coordenar concorrência pelo Projeto, conforme os requisitos.
- Exclusão de Projeto e suas tarefas visíveis é atômica.
- O controle de versão inicia em `1`; mutações efetivas incrementam a versão. Operações idempotentes não a incrementam.
- Status e Responsável verificam referências inclusive em tarefas excluídas logicamente quando a regra exigir.
- Secrets nunca vão para `appsettings.json`; desenvolvimento usa User Secrets e ambientes hospedados usam variáveis secretas.

## 7. Convenções de código

- Código, namespaces, classes e métodos: inglês técnico ou nomes de domínio já consolidados no projeto; manter consistência dentro de cada recurso.
- Classes e métodos públicos devem expressar intenção de domínio, não detalhes de interface ou banco.
- `async` é obrigatório para I/O; métodos de Domain são síncronos.
- Receber `CancellationToken` em handlers, queries e métodos de repositório assíncronos.
- Não usar `DateTime.Now`; receber horário UTC por `IRelogio` na Application e passar o valor ao Domain.
- Não expor senha, hash, token, `UsuarioId` informado pelo cliente ou detalhes internos de Identity em respostas de domínio.
- Não alterar o contrato OpenAPI sem atualizar simultaneamente MVC, Clean Architecture, testes de contrato e documentação.

## 8. Testes e equivalência com MVC

- Domain: testes unitários para matriz de status, conclusão, reabertura, exclusão lógica e invariantes.
- Application: testes unitários para autorização contextual, orquestração e falhas de regra.
- Infrastructure: testes de integração com PostgreSQL real para EF Core, filtros, transações, índices e concorrência.
- WebAPI: testes de contrato HTTP parametrizados pela URL e executados contra ambas as APIs.
- Todo cenário CA01-CA34 deve ter o mesmo comportamento observável nas duas arquiteturas.
- Métricas de complexidade, manutenibilidade, cobertura e pipeline devem ser comparáveis: mesmas regras, mesma base de dados isolada, mesma carga e versões congeladas.

## 9. Checklist antes de concluir uma alteração

- [ ] A mudança pertence à camada em que foi implementada?
- [ ] As dependências continuam apontando para dentro?
- [ ] Uma regra de negócio foi colocada em Domain ou Application, e não em Controller/repositório?
- [ ] O contrato OpenAPI e o comportamento MVC continuam equivalentes?
- [ ] Exclusão lógica, versão e UTC foram considerados quando aplicáveis?
- [ ] Segredos ficaram fora do código e de `appsettings.json`?
- [ ] Testes adequados foram criados ou atualizados?
- [ ] `dotnet build` e os testes relevantes foram executados?
- [ ] A alteração foi associada ao work item do Azure DevOps?

## 10. Decisão em caso de dúvida

Quando uma regra puder ficar em mais de uma camada, escolher a opção mais interna que não exija conhecimento externo:

1. Se depende somente do estado do agregado, fica no **Domain**.
2. Se precisa coordenar agregados, autorização, repositórios ou transação, fica na **Application**.
3. Se depende de EF Core, PostgreSQL, Identity, JWT, filesystem ou rede, fica na **Infrastructure**.
4. Se depende de HTTP, rota, header, status code ou serialização, fica na **WebAPI**.

Alterações que não se encaixarem nesse padrão devem ser discutidas antes da implementação para preservar a validade da comparação do TCC.
