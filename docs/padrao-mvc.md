# Padrão de implementação — MVC

Este documento define o padrão de implementação e operação da API `TaskManager.MVC`. Ele deve ser consultado antes de criar ou alterar código no projeto `TaskManager.MVC.API`.

O TCC compara uma implementação MVC monolítica com uma implementação em Clean Architecture do mesmo gerenciador de tarefas. A organização interna pode ser diferente, mas o comportamento externo deve permanecer equivalente: rotas, contratos, regras de negócio, respostas HTTP, códigos de erro e critérios de aceitação são comuns às duas APIs.

Fontes normativas do projeto:

- [Requisitos funcionais](requisitos-funcionais.md): regras de negócio e critérios de aceitação CA01–CA34.
- [Contrato OpenAPI](openapi/taskmgr-v1.yaml): contrato HTTP comum às duas APIs.
- [Casos de uso](diagramas/casos-de-uso.md): atores e operações do sistema.
- [Configuração local](configuracao-local.md): infraestrutura PostgreSQL compartilhada.
- [Padrão Clean Architecture](padrao-clean-architecture.md): referência da implementação usada na comparação.

Em caso de divergência, o contrato OpenAPI e os requisitos funcionais prevalecem sobre este guia.

## 1. Objetivo arquitetural

A API usa ASP.NET Core MVC em um único projeto executável. A separação por pastas organiza responsabilidades sem simular camadas ou criar abstrações que o monólito não necessita.

```text
HTTP request
  -> Controller
  -> Service
  -> Model + DbContext/Identity
  -> PostgreSQL
```

O fluxo inverso transforma o resultado em contrato HTTP ou `Problem Details`. Controllers devem permanecer finos; regras de negócio, autorização contextual e coordenação transacional ficam nos services. Modelos representam o estado persistido e o `DbContext` concentra o mapeamento e o controle de versão.

## 2. Estrutura do projeto

```text
src/TaskManager.MVC/TaskManager.MVC.API/
├── Configuration/   # DI, autenticação, Problem Details e handlers globais
├── Contracts/       # requests e responses HTTP, agrupados por recurso
├── Controllers/     # rotas, autorização global e tradução HTTP
├── Data/            # DbContext, bloqueios, mapeamentos e migrations
├── Identity/        # contas, usuário atual, JWT e Gestor inicial
├── Models/          # entidades persistidas e enum de categoria de status
├── Services/        # casos de uso e regras por recurso
└── Program.cs       # composição e pipeline HTTP
```

Não criar projetos, interfaces ou repositórios apenas para reproduzir a estrutura da Clean Architecture. Uma abstração só se justifica quando houver uma fronteira técnica real, mais de uma implementação necessária ou benefício comprovado para testes.

## 3. Responsabilidades

### Controllers

- Declaram rota, verbo, binding, autenticação e papel global exigido.
- Recebem contratos HTTP, chamam um service e devolvem o status correto.
- Não acessam `TaskManagerMvcDbContext`, Identity ou SQL diretamente.
- Não implementam regras de transição, concorrência ou autorização sobre um recurso.
- Propagam `CancellationToken` para toda operação assíncrona.

### Contracts

- Definem requests e responses compatíveis com `docs/openapi/taskmgr-v1.yaml`.
- Aplicam validações estruturais com Data Annotations; validações dependentes de estado ficam nos services.
- Nunca expõem entidades do EF Core, hash de senha, identificadores internos do Identity ou detalhes de persistência.
- Listagens paginadas usam `PageResponse<T>` e os parâmetros `pagina` e `tamanho`.

### Services

- Implementam casos de uso, regras de negócio e autorização contextual.
- Consultam o `DbContext`, carregam relacionamentos, verificam a versão e persistem a operação.
- Usam transação explícita e bloqueio quando uma decisão depende do estado concorrente de Projeto ou Responsável.
- Lançam `ApiException` com status e código estáveis para falhas esperadas.
- Projetam entidades para contratos de resposta; não retornam `IQueryable` aos controllers.

Os services atuais são `ProjectService`, `TaskService`, `StatusService`, `AccountService` e `AuthenticationService`. Regras compartilhadas pequenas, como a matriz de transições, podem ficar em uma classe interna específica (`TaskStatusTransition`), sem criar uma nova camada.

### Models

- Representam `Projeto`, `Tarefa`, `Responsavel` e `StatusTarefa` conforme persistidos.
- `CategoriaStatus`, e não nome ou ID fixo, define a semântica de uma transição.
- Entidades versionadas implementam `IVersionado`.
- Projetos e tarefas usam `ExcluidoEm` para exclusão lógica; tarefas concluídas usam `ConcluidaEm`.

### Data

- `TaskManagerMvcDbContext` integra as entidades da aplicação e ASP.NET Core Identity.
- Mapeamentos EF Core definem tabelas, limites, índices, filtros globais, chaves estrangeiras e token de concorrência.
- `TransactionalLock` encapsula `SELECT ... FOR UPDATE` e só pode ser usado dentro de transação ativa.
- A factory de design lê User Secrets e variáveis de ambiente para migrations.
- Migrations são o histórico versionado do schema; não editar migrations já aplicadas.

### Identity e Configuration

- Identity gerencia conta, senha, estado ativo e papéis `Usuario` e `Gestor`.
- `AuthenticationService` valida as credenciais e `TokenService` emite o JWT.
- `CurrentUser` obtém ID e papel das claims validadas.
- A validação do token recarrega o usuário e os papéis no banco, recusando contas inativas.
- `IdentityBootstrapper` garante os papéis e cria opcionalmente o Gestor inicial.
- Handlers globais convertem exceções conhecidas e conflitos de concorrência em `application/problem+json`.

## 4. Superfície HTTP

Todas as rotas abaixo, exceto login e health check, exigem JWT Bearer. Operações marcadas como Gestor exigem a claim de papel `Gestor`; as demais ainda podem aplicar autorização contextual no service.

| Recurso | Operações | Acesso |
| --- | --- | --- |
| Autenticação | `POST /auth/login` | Anônimo |
| Projetos | `GET /projetos`, `GET /projetos/{id}`, `POST /projetos`, `PUT /projetos/{id}`, `DELETE /projetos/{id}` | Autenticado |
| Tarefas | listagem, detalhe, criação, atualização e exclusão em `/tarefas` | Autenticado |
| Responsável da tarefa | `PUT /tarefas/{id}/responsavel`, `DELETE /tarefas/{id}/responsavel` | Autenticado, sujeito à regra do recurso |
| Status da tarefa | `PATCH /tarefas/{id}/status` | Autenticado, sujeito à transição e ao papel |
| Status | consulta em `/status`; criação, atualização e exclusão | Consulta autenticada; mutação por Gestor |
| Responsáveis | consulta em `/responsaveis`; criação, atualização, desativação e exclusão | Consulta autenticada; mutação por Gestor |
| Operação | `GET /health` | Anônimo |

Projetos e tarefas aceitam paginação com padrão `pagina=1` e `tamanho=20`; o tamanho máximo é 100. A listagem de tarefas também aceita `projetoId`.

O detalhamento de campos, formatos, respostas e exemplos pertence ao contrato OpenAPI, que evita duplicação e deve ser atualizado junto com as duas APIs.

## 5. Fluxo de um caso de uso

```text
ASP.NET Core valida autenticação e papel global
  -> Controller valida formato e binding do request
  -> Service carrega o estado necessário
  -> Service valida existência, autorização contextual e versão
  -> Service aplica regras de negócio
  -> DbContext persiste a alteração, dentro de transação quando necessário
  -> Service projeta o response
  -> Controller define o status HTTP
```

A ordem observável de validação é: autenticação, permissão global, corpo da requisição, existência do alvo, permissão sobre o alvo, versão e regras de negócio. Na mudança de status, validar a matriz de transição antes da exigência de responsável ativo.

## 6. Regras centrais

- Apenas Gestor administra Status e contas de Responsáveis.
- Um usuário comum só altera recursos permitidos pelos critérios de aceitação e pela atribuição da tarefa.
- Reabertura de tarefa concluída exige Gestor.
- Transições usam as categorias `Pendente`, `EmAndamento` e `Concluida`.
- Uma tarefa não entra em andamento ou conclusão sem Responsável ativo quando a regra funcional assim exigir.
- Status protegidos não podem ser excluídos; código e categoria são imutáveis depois da criação.
- Status ou Responsável referenciado por tarefa, inclusive excluída quando previsto, não pode ser removido.
- O último Gestor ativo não pode ser desativado, excluído ou rebaixado; o usuário atual não pode invalidar a própria conta pelas operações protegidas.
- Excluir um Projeto e suas tarefas visíveis é uma operação atômica e respeita a restrição de tarefas em andamento.
- Operações idempotentes não devem incrementar versão nem produzir alteração persistida artificial.

Os critérios CA01–CA34 em `requisitos-funcionais.md` são a especificação completa dessas regras.

## 7. Persistência e concorrência

- PostgreSQL usa o banco isolado `taskmgr_mvc`.
- Filtros globais ocultam Projetos e Tarefas com `ExcluidoEm`; verificações de integridade usam `IgnoreQueryFilters()` quando precisam considerar o histórico.
- Chaves estrangeiras são restritivas, evitando exclusão em cascata acidental.
- `StatusTarefa.Codigo` e `Responsavel.UsuarioId` são únicos.
- A migration inicial cria os status protegidos `PENDENTE`, `EM_ANDAMENTO` e `CONCLUIDA` com IDs determinísticos; regras nunca dependem desses IDs.
- `Versao` inicia em 1 e é token de concorrência otimista. Alterações rastreadas incrementam a versão no `SaveChanges`; o request deve informar a versão esperada.
- Incompatibilidade de versão ou `DbUpdateConcurrencyException` resulta em HTTP 409 com código `CONCORRENCIA`.
- Criação de tarefa, mudança de status, exclusão de projeto e proteção do último Gestor coordenam decisões críticas com transação e bloqueio pessimista quando aplicável.
- Datas persistidas são UTC. Não usar `DateTime.Now`.

## 8. Autenticação e erros

O login recebe e-mail e senha e devolve um access token JWT válido por 30 minutos. Não há refresh token. O emissor, audiência e chave são obrigatórios; a chave deve possuir ao menos 32 bytes.

Falhas usam `application/problem+json` com `type`, `title`, `status`, `detail`, `instance` e a extensão `code`. Códigos estáveis incluem:

- `DADOS_INVALIDOS`, `NAO_AUTENTICADO`, `ACESSO_NEGADO` e `RECURSO_NAO_ENCONTRADO`;
- `VALOR_DUPLICADO`, `REGISTRO_EM_USO` e `REGISTRO_PROTEGIDO`;
- `TRANSICAO_INVALIDA`, `CONCORRENCIA` e demais códigos definidos pelos requisitos.

Erros esperados devem usar `ApiException`; falhas de concorrência do EF Core são tratadas pelo handler específico. Exceções inesperadas permanecem sob o `ProblemDetails` padrão e não devem expor stack trace ou segredo.

## 9. Configuração e execução local

Pré-requisitos: SDK definido em `global.json`, Docker com PostgreSQL e `dotnet-ef` compatível com EF Core 10 para manipular migrations.

Após subir o banco conforme `configuracao-local.md`, configure os segredos:

```powershell
dotnet user-secrets set "ConnectionStrings:TaskManager" "Host=localhost;Port=5432;Database=taskmgr_mvc;Username=taskmgr;Password=<sua-senha>" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "Jwt:Emissor" "TaskMgr" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "Jwt:Audiencia" "TaskMgr.React" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "Jwt:ChaveSecreta" "<chave-local-com-no-minimo-32-bytes>" --project src/TaskManager.MVC/TaskManager.MVC.API
```

Opcionalmente, provisione o primeiro Gestor informando os três valores; configuração parcial impede a inicialização:

```powershell
dotnet user-secrets set "GestorInicial:Nome" "<nome>" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "GestorInicial:Email" "<email>" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "GestorInicial:Senha" "<senha-segura>" --project src/TaskManager.MVC/TaskManager.MVC.API
```

Em ambientes hospedados, use variáveis com `__` como separador, por exemplo `ConnectionStrings__TaskManager` e `Jwt__ChaveSecreta`. Segredos nunca devem ser gravados em `appsettings.json`.

Para aplicar migrations, compilar e executar:

```powershell
dotnet ef database update --project src/TaskManager.MVC/TaskManager.MVC.API --startup-project src/TaskManager.MVC/TaskManager.MVC.API --context TaskManagerMvcDbContext
dotnet build src/TaskManager.MVC/TaskManager.MVC.sln
dotnet run --project src/TaskManager.MVC/TaskManager.MVC.API --launch-profile http
```

A API HTTP local usa `http://localhost:5129`. `GET /health` verifica a conexão com o banco. Em Development, Swagger UI fica em `/swagger` e os documentos OpenAPI gerados ficam disponíveis pela configuração do ASP.NET Core.

## 10. Convenções de manutenção

- Preservar os nomes públicos definidos pelo OpenAPI, mesmo quando classes internas usam inglês técnico.
- Usar `async` em I/O e propagar `CancellationToken`.
- Manter consultas somente leitura com `AsNoTracking()` quando não houver atualização posterior.
- Projetar respostas explicitamente; não serializar entidades rastreadas.
- Fazer uma única transação por caso de uso atômico e nunca executar bloqueio fora dela.
- Não adicionar mediator, repository genérico, unit of work própria ou mapeador automático sem necessidade demonstrada.
- Não alterar o contrato sem atualizar MVC, Clean Architecture, testes de contrato e documentação no mesmo trabalho.
- Criar migration somente para mudança do modelo persistido e revisar o SQL/arquivo gerado antes de versionar.

## 11. Testes e equivalência

- Testes unitários cobrem regras isoladas, especialmente matriz de transições e validações sem banco.
- Testes de integração usam PostgreSQL real para mapeamentos, filtros, índices, Identity, transações e concorrência.
- Testes HTTP cobrem autenticação, autorização, binding, status, headers, responses e Problem Details.
- A suíte de contrato deve ser parametrizada pela URL e executar os mesmos CA01–CA34 contra MVC e Clean Architecture.
- As comparações de métricas usam o mesmo conjunto de requisitos, versões e carga para as duas arquiteturas.

Validação mínima antes de concluir uma alteração:

```powershell
dotnet build src/TaskManager.MVC/TaskManager.MVC.API/TaskManager.MVC.API.csproj
```

Execute também os testes unitários da solução, que não dependem de banco:

```powershell
dotnet test src/TaskManager.MVC/TaskManager.MVC.sln
```

## 12. Checklist antes de concluir uma alteração

- [ ] O comportamento continua compatível com o OpenAPI e os requisitos CA01–CA34?
- [ ] O controller apenas adapta HTTP e delega o caso de uso?
- [ ] Regra de negócio, autorização contextual e transação ficaram no service apropriado?
- [ ] Exclusão lógica, referências históricas, UTC e versão foram consideradas?
- [ ] A operação concorrente está protegida no menor escopo necessário?
- [ ] A resposta usa contrato próprio e falhas usam Problem Details com código estável?
- [ ] Segredos ficaram fora do repositório?
- [ ] Testes relevantes, build e suíte de contrato foram executados?
- [ ] A equivalência observável com a API Clean Architecture foi preservada?

## 13. Decisão em caso de dúvida

Escolha o local mais simples que mantenha a responsabilidade correta:

1. Formato HTTP, rota, status ou header: **Controller/Contract**.
2. Regra, permissão contextual ou coordenação de operação: **Service**.
3. Estado persistido e comportamento local simples da entidade: **Model**.
4. EF Core, SQL, mapping, transaction ou migration: **Data**.
5. Conta, senha, claims, papel ou token: **Identity**.
6. Registro de dependências, middleware ou tratamento global: **Configuration/Program**.

Se uma mudança exigir nova camada ou abstração, primeiro confirme que a estrutura atual não atende. O objetivo do MVC neste estudo é manter um monólito organizado e diretamente comparável, não reproduzir a Clean Architecture dentro de um único projeto.
