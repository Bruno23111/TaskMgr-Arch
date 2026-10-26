# Testes de contrato — TaskMgr-Arch

Suíte HTTP caixa-preta que executa **o mesmo código de teste** contra as duas APIs (MVC e Clean Architecture) para verificar os critérios de aceitação CA01–CA34 de [docs/requisitos-funcionais.md](../../docs/requisitos-funcionais.md) e o contrato [docs/openapi/taskmgr-v1.yaml](../../docs/openapi/taskmgr-v1.yaml).

É o critério de conclusão funcional da seção 6 dos requisitos: todos os cenários aprovados nas duas APIs, sem divergência não justificada.

## Como executar

Pré-requisitos:
- Docker em execução.
- SDK .NET 10.0.401, fixado em `global.json`.

```bash
dotnet test tests/TaskManager.ContractTests
```

Filtros úteis:

```bash
dotnet test tests/TaskManager.ContractTests --filter "FullyQualifiedName~.Mvc."        # só a API MVC
dotnet test tests/TaskManager.ContractTests --filter "FullyQualifiedName~.CleanArch."  # só a Clean Architecture
dotnet test tests/TaskManager.ContractTests --filter "CA=CA27"                         # um critério de aceitação
dotnet test tests/TaskManager.ContractTests --filter "RN=RN06"                         # uma regra de negócio
```

Na primeira execução o Testcontainers baixa a imagem `postgres:15-alpine`. Depois disso, a suíte inteira leva cerca de 1 minuto.

## Como funciona

```
Cenarios/            Cenários escritos uma única vez (classes abstratas)
Implementacoes/      Uma linha por cenário e API: CriacaoDeTarefaMvc, CriacaoDeTarefaCleanArch...
Infraestrutura/      Container PostgreSQL, hospedagem das APIs e reinício do banco
Cliente/             Cliente HTTP, modelos do contrato e asserções
```

- **Caixa-preta.**
  - Os cenários conversam com as APIs apenas por HTTP.
  - Os modelos em `Cliente/Modelos.cs` foram escritos a partir do OpenAPI, sem reutilizar tipos das APIs.
  - A leitura é estrita: um campo fora do schema, um campo `required` ausente ou um `null` indevido reprova o teste.
- **Hospedagem.**
  - Cada API roda em memória (`WebApplicationFactory`) com configuração idêntica: JWT, Gestor inicial e ambiente `Testing`, que não lê os user-secrets da máquina.
  - As duas classes `Program` são distinguidas por `extern alias` (`mvc::` e `cleanarch::`).
- **Banco real e isolado.**
  - Um único container PostgreSQL 15 atende a execução toda; cada API tem bancos próprios dentro dele.
  - Na inicialização, o banco de trabalho é migrado e a API cria os papéis e o Gestor inicial. Esse estado é copiado para um banco-modelo.
  - **Antes de cada cenário**, o banco de trabalho é apagado e recriado a partir do modelo (`CREATE DATABASE ... TEMPLATE`). Os dados iniciais são sempre os mesmos, como exige a seção 6.
- **Execução.** Os cenários de uma mesma API rodam em série; as duas APIs rodam em paralelo.
- **Acesso direto ao banco.** É usado só onde a API não expõe o estado:
  - para conferir a exclusão lógica (`ExcluidoEm`);
  - para conferir invariantes de concorrência;
  - para provocar uma falha de persistência no CA17, com um trigger.
- **Concorrência.** As duas requisições são liberadas juntas por uma largada explícita, sem `sleep`. Cada cenário repete a disputa 10 vezes e verifica invariantes do estado final, não uma ordem de chegada.

### Decisões de interpretação dos requisitos

| Situação | Decisão |
| --- | --- |
| IDs dos status do seed | Diferem entre as APIs (MVC `00000000-…-000N`, CleanArch `NNNNNNNN-…`). Os testes os resolvem pelo `codigo`, pois as regras não podem depender de ID fixo (seção 1) |
| Quem perde uma disputa (CA27, CA28) | Aceita-se o código de negócio da situação **ou** `409 CONCORRENCIA`. O requisito exige resultado serializável, não um código específico. HTTP 5xx nunca é aceito |
| `concluidaEm` | A resposta logo após a gravação tem precisão de 100 ns; o PostgreSQL guarda microssegundos. Comparações de data usam microssegundos |
| Normalização não especificada | Os testes não exigem remoção de espaços nem conversão de código para maiúsculas, porque o contrato não especifica esses comportamentos |

## Rastreabilidade

| Critério | Classe de cenários |
| --- | --- |
| CA01, CA02 | `CriacaoDeTarefa` |
| CA03–CA10, CA24, CA25, matriz completa (RN02–RN05) | `TransicoesDeStatus` |
| CA11, CA12 (todas as ações exclusivas do Gestor, com corpo válido e inválido) | `Permissoes` |
| CA13, CA14 | `Autenticacao` |
| CA15, CA16, CA17 | `ExclusaoDeProjeto` |
| CA18, RN06 | `TarefaConcluidaEExclusao` |
| CA19–CA22, CA31, CA33 (e-mail) | `ResponsaveisEContas` |
| CA23, CA24 (renomear), CA33 (código), proteção do seed | `CatalogoDeStatus` |
| CA29, CA30, CA32 (RN07) | `ValidacaoDeEntrada` |
| CA26, CA27, CA28 | `Concorrencia` |
| CA34 (CRUD de cada entidade, paginação, listas vazias) | `CrudCompleto` |

Cada teste tem um `[Trait("CA", ...)]` ou `[Trait("RN", ...)]` que o liga ao requisito.
