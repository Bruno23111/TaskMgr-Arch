# Testes unitários — MVC

Testes das regras de negócio da API MVC **sem PostgreSQL**.

```bash
dotnet test tests/TaskManager.MVC/TaskManager.MVC.UnitTests
```

A suíte roda em cerca de 1 segundo e não precisa de Docker.

## Organização

```
TaskStatusTransitionTests.cs   Matriz de transições (RN02, RN04) — a única regra isolada do DbContext
ProjectServiceTests.cs         ProjectService
TaskServiceTests.cs            TaskService
StatusServiceTests.cs          StatusService
Infraestrutura/                DbContext da API sobre EF Core InMemory, usuário atual e asserções de ApiException
```

## Como funciona

- **Regra pura.** `TaskStatusTransition` é uma função pura e é testada diretamente.
- **Services.** As demais regras ficam nos Services, que recebem o `TaskManagerMvcDbContext` concreto.
  - Sem uma abstração entre eles, o único substituto possível do PostgreSQL é o provedor **EF Core InMemory**.
  - O `DbContext` usado é o da própria API, com os mesmos mapeamentos, filtros de exclusão lógica e seed de status.
- **Usuário atual.** O `CurrentUser` é montado a partir de um `ClaimsPrincipal` com o papel desejado.

## Limitação: operações transacionais

O provedor InMemory não tem transações nem SQL. Por isso, as operações que abrem transação serializável e bloqueiam linhas com `SELECT ... FOR UPDATE` **só podem ser testadas até a verificação que antecede a transação** (permissão e dados de entrada):

| Operação | Coberto aqui | Exige PostgreSQL |
| --- | --- | --- |
| `TaskService.CriarAsync` | permissão, dados | projeto existente, status inicial, gravação |
| `TaskService.AtribuirResponsavelAsync` | permissão, dados | responsável inativo, tarefa concluída, gravação |
| `TaskService.AlterarStatusAsync` | dados | permissão contextual, RN03, conclusão e reabertura, idempotência |
| `ProjectService.ExcluirAsync` | permissão, dados | RN01, exclusão em conjunto e rollback |

Esses caminhos são cobertos pelos testes de contrato (`tests/TaskManager.ContractTests`). Na Clean Architecture, a mesma orquestração fica nos handlers, que dependem só de interfaces, e é testada por inteiro nos testes unitários (`tests/TaskManager.CleanArch/TaskManager.CleanArch.UnitTests`).

A permissão de Gestor no `StatusService` fica no atributo `[Authorize]` do controller, fora do Service. Por isso não há teste de 403 para status.

Cada teste tem `[Trait("CA", ...)]` ou `[Trait("RN", ...)]` ligando-o a `docs/requisitos-funcionais.md`.
