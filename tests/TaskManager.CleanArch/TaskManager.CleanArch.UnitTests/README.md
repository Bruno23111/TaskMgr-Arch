# Testes unitários — Clean Architecture

Testes das regras de negócio da API Clean Architecture **sem banco, sem EF Core, sem Identity e sem ASP.NET Core**. O projeto referencia apenas `Domain` e `Application`.

```bash
dotnet test tests/TaskManager.CleanArch/TaskManager.CleanArch.UnitTests
```

A suíte roda em menos de 1 segundo e não precisa de Docker.

## Organização

```
Domain/        Entidades puras: matriz de transições, conclusão, reabertura, exclusão lógica e invariantes
Application/   Handlers do MediatR: autorização contextual, orquestração e falhas de regra
  Falsos/      Implementações em memória das portas da Application (repositórios, unidade de trabalho,
               contas, relógio, despachante de eventos, serviço de token)
Regras.cs      Asserção de violação de regra de domínio pelo código público do contrato
```

## Como funciona

- **Domain.** As entidades recebem as categorias de status já resolvidas e a data em UTC. Os testes chamam os métodos diretamente e conferem estado, versão e eventos de domínio.
- **Application.** Cada handler é montado à mão com as implementações falsas de `Application/Falsos`.
  - São escritas à mão, sem biblioteca de mock.
  - Têm a mesma semântica dos repositórios reais da Infrastructure: registros excluídos ficam ocultos, exceto nas verificações de referência, que também consideram os excluídos.
- **Estado inicial.** `Ambiente` cria os três status protegidos do seed e um Gestor autenticado, como o banco recém-criado da API.
- **Orquestração.** `UnidadeDeTrabalhoFalsa` registra quantas gravações o handler pediu e em qual nível de transação. Isso permite verificar, sem banco, que uma rejeição não grava nada e que as operações sensíveis a concorrência usam transação serializável.

## O que fica de fora

O que depende da Infrastructure:
- bloqueio `SELECT ... FOR UPDATE`;
- tradução de erros do PostgreSQL;
- Identity e JWT.

Esses comportamentos são cobertos pelos testes de contrato (`tests/TaskManager.ContractTests`).

Cada teste tem `[Trait("CA", ...)]` ou `[Trait("RN", ...)]` ligando-o a `docs/requisitos-funcionais.md`.
