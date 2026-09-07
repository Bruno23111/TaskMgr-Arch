# Requisitos funcionais — TaskMgr-ArchPerf

Este documento consolida os requisitos para desenvolvimento; não indica funcionalidades já implementadas. O escopo funcional e os contratos HTTP serão iguais em MVC e Clean Architecture.

## 1. Status e transições

Cada Tarefa pertence a exatamente um Projeto, tem exatamente um Status e pode ter zero ou um Responsável. Uma tarefa nova recebe automaticamente o status inicial `PENDENTE`; o cliente não escolhe outro status na criação.

Status é uma entidade de catálogo, com código único, nome e categoria semântica. As categorias são fixas: `PENDENTE`, `EM_ANDAMENTO` e `CONCLUIDA`. As regras usam a categoria, nunca o nome exibido ou um ID numérico fixo.

O seed cria três status protegidos, um por categoria, com os códigos acima. O Gestor pode renomeá-los, mas não alterar código, categoria ou excluí-los. Pode criar status adicionais, com código e categoria imutáveis após a criação, preservando o CRUD de Status previsto no artigo. Status adicionais não alteram a matriz de regras.

| Categoria atual / destino | PENDENTE | EM_ANDAMENTO | CONCLUIDA |
| --- | --- | --- | --- |
| PENDENTE | Permitido | Permitido | Proibido |
| EM_ANDAMENTO | Permitido | Permitido | Permitido, com responsável ativo |
| CONCLUIDA | Somente Gestor, reabertura | Proibido | Permitido, com responsável ativo |

As permissões da seção 2 também se aplicam. Na diagonal, mudar para outro status da mesma categoria é permitido; enviar o mesmo `StatusId` é uma operação idempotente, sem gravação ou incremento de versão. Autenticação, autorização e versão esperada continuam sendo verificadas antes desse retorno.

- RN01: impedir exclusão de Projeto se qualquer tarefa não excluída estiver na categoria `EM_ANDAMENTO`.
- RN02: rejeitar mudanças fora da matriz, sem alterar a tarefa.
- RN03: exigir responsável ativo ao entrar na categoria `CONCLUIDA`. Atribuição e conclusão são operações separadas.
- RN04: reabrir significa mudar de `CONCLUIDA` para `PENDENTE`, somente por Gestor. Preserva o responsável, se houver, e limpa `ConcluidaEm`.
- RN05: preencher `ConcluidaEm` com horário UTC do servidor ao entrar em conclusão; preservar o valor ao mudar entre status da categoria concluída.
- RN06: tarefas concluídas não permitem editar conteúdo, trocar/remover responsável ou excluir individualmente. É necessário reabrir primeiro. A mudança entre status da categoria concluída continua permitida pela matriz.
- RN07: status e responsável só são alterados pelas operações específicas; edição genérica não pode contornar essas validações. Não se transfere tarefa entre projetos nesta versão.

Iniciar trabalho sem responsável é permitido ao Gestor. Isso mantém RN03 verificável de forma independente: uma tarefa pode estar em andamento e ainda assim não poder ser concluída.

## 2. Identidade e autorização

Autenticação aprovada: ASP.NET Core Identity local, persistido em PostgreSQL, com login por e-mail e senha e emissão explícita de JWT pela aplicação. Identity gerencia contas e hashes de senha; a API valida assinatura, emissor, audiência e expiração do JWT. Os tokens bearer prontos dos endpoints de Identity não devem ser confundidos com JWT.

Para o protótipo, o access token dura 30 minutos, sem refresh token; expirada a sessão, faz-se novo login. O logout remove o token do cliente. Toda requisição protegida verifica também se a conta está ativa e considera o papel atual no servidor, para que desativação e alteração de papel tenham efeito imediato. Cadastro público fica fora do escopo; o Gestor cria contas. A conta inicial de Gestor é provisionada por configuração, sem senha fixa no código.

Responsável é o perfil de domínio associado a uma conta, em relação 1:1 por `UsuarioId` único. A criação do perfil e da conta é atômica. Tanto Usuário quanto Gestor podem ser responsáveis por tarefas. O perfil não contém senha, token nem herda classes de Identity. Na Clean Architecture, Domain contém apenas o identificador da conta; detalhes de Identity ficam em Infrastructure.

O protótipo tem um único espaço compartilhado: todas as contas ativas consultam todos os projetos e tarefas. Não há membros por projeto nem isolamento por organização nesta versão. Nome e identificador dos responsáveis são consultáveis, sem expor credenciais ou dados internos de autenticação.

| Operação | Usuário | Gestor |
| --- | --- | --- |
| Consultar projetos, tarefas, responsáveis e status | Sim | Sim |
| Criar, editar e excluir Projeto | Não | Sim |
| Adicionar e excluir Tarefa | Não | Sim |
| Editar conteúdo de Tarefa não concluída | Somente quando for o responsável atual | Sim |
| Alterar Status de Tarefa atribuída a si | Conforme matriz, exceto reabertura | Conforme matriz |
| Alterar Status de tarefa alheia ou sem responsável | Não | Conforme matriz |
| Atribuir, trocar ou remover responsável | Não | Sim, em tarefa não concluída |
| Criar, editar, desativar ou excluir Responsável/conta | Não | Sim, respeitando restrições |
| Criar, editar ou excluir Status | Não | Sim, respeitando restrições |
| Alterar papéis de contas | Não | Sim; não remover o último Gestor ativo |

O servidor identifica o usuário pelo token validado, nunca por `UsuarioId` informado no corpo da requisição. Ser criador de uma tarefa não concede permissão adicional. Um Gestor não pode excluir/desativar a própria conta; a exclusão, desativação ou rebaixamento do último Gestor ativo é bloqueada.

## 3. Exclusão e integridade

Adota-se exclusão lógica de Projetos e Tarefas, com `ExcluidoEm` em UTC. Registros excluídos não aparecem em consultas normais e são tratados como inexistentes nas operações da API. Restauração fica fora do escopo desta versão.

| Entidade / operação | Comportamento |
| --- | --- |
| Excluir Projeto com tarefa em andamento | Rejeitar com conflito; não excluir projeto nem tarefas |
| Excluir Projeto sem tarefas em andamento | Marcar projeto e todas as suas tarefas ainda visíveis como excluídos, atomicamente |
| Excluir Projeto com tarefas concluídas | Permitido; a exclusão em conjunto é exceção explícita à proibição de excluir individualmente uma tarefa concluída |
| Excluir Tarefa pendente ou em andamento | Permitido ao Gestor; excluir logicamente |
| Excluir Tarefa concluída individualmente | Rejeitar; reabrir primeiro |
| Excluir Responsável sem nenhuma tarefa vinculada | Excluir perfil e conta atomicamente, respeitando proteção dos gestores |
| Excluir Responsável com tarefa vinculada, inclusive excluída | Rejeitar; nunca apagar nem desatribuir tarefas automaticamente |
| Desativar Responsável | Rejeitar se houver tarefa visível pendente ou em andamento atribuída a ele; primeiro reatribuir/remover essas atribuições |
| Desativar Responsável com apenas tarefas concluídas ou excluídas | Permitido, preservando referências; conta não pode autenticar nem receber novas atribuições |
| Excluir Status adicional sem referência | Exclusão física permitida |
| Excluir Status referenciado por qualquer tarefa, inclusive excluída | Rejeitar; nunca alterar status das tarefas em cascata |
| Excluir Status protegido do seed | Rejeitar mesmo sem tarefas vinculadas |

A existência de tarefas concluídas vinculadas a um responsável que posteriormente ficou inativo não invalida retroativamente a conclusão. RN03 é verificada na transição. Ao reabrir tal tarefa, o Gestor deve atribuir um responsável ativo antes de concluí-la novamente.

As consultas de integridade que protegem exclusão de Responsável e Status consideram também registros excluídos logicamente. Chaves estrangeiras não usam cascata de exclusão para esses vínculos.

## 4. Contrato observável das APIs

| Situação | HTTP | Código de erro / resultado |
| --- | --- | --- |
| Criação válida | 201 | Representação criada e `Location` |
| Consulta ou alteração válida | 200 | Representação atual; listas vazias retornam coleção vazia |
| Exclusão válida | 204 | Sem corpo |
| Corpo inválido, campo obrigatório ausente, referência informada inexistente | 400 | `DADOS_INVALIDOS` |
| Login inválido, token ausente/inválido/expirado ou conta desativada | 401 | `NAO_AUTENTICADO` |
| Usuário autenticado sem permissão, inclusive reabertura por Usuário | 403 | `ACESSO_NEGADO` |
| Recurso alvo inexistente ou excluído | 404 | `RECURSO_NAO_ENCONTRADO` |
| Transição proibida | 409 | `TRANSICAO_INVALIDA` |
| Conclusão sem responsável ativo | 409 | `RESPONSAVEL_OBRIGATORIO` |
| Projeto com tarefa em andamento | 409 | `PROJETO_COM_TAREFA_EM_ANDAMENTO` |
| Exclusão de Responsável ou Status referenciado | 409 | `REGISTRO_EM_USO` |
| Alteração proibida em tarefa concluída | 409 | `TAREFA_CONCLUIDA` |
| Alteração/exclusão de status protegido ou proteção de conta Gestor | 409 | `REGISTRO_PROTEGIDO` |
| Atribuição a responsável inativo | 409 | `RESPONSAVEL_INATIVO` |
| Código de status ou e-mail duplicado | 409 | `VALOR_DUPLICADO` |
| Versão desatualizada / conflito de gravação | 409 | `CONCORRENCIA` |

Erros usam `application/problem+json`, com `type`, `title`, `status`, `detail`, `instance` e extensão estável `code`. Validar autenticação primeiro, depois permissão global, corpo da requisição, existência do alvo, permissão sobre o alvo, versão e regras de negócio. Na alteração de status, validar a matriz antes de RN03. Para múltiplos problemas de campo, retornar todos em `errors`.

Atualizações e exclusões exigem a versão lida pelo cliente: `versao` no corpo de alterações e parâmetro `versao` na exclusão. A ausência é erro 400. A criação inicia a versão em 1; mutações efetivas incrementam a versão. Senhas, hashes e tokens não integram representações de domínio ou mensagens de erro.

Operações relacionadas são atômicas. Apenas uma transação ao salvar não resolve, por si só, disputas entre verificar uma regra e gravar: exclusão de projeto, criação de tarefa e transição de status precisam de coordenação consistente por projeto. Na implementação, adotar bloqueio transacional da linha do Projeto em todas essas operações; manter controle de versão para conflitos de edição. Operações envolvendo conta, responsável e referências também devem impedir atribuição concorrente a um perfil em desativação/exclusão.

## 5. Critérios de aceitação comuns

Cada cenário deve executar contra ambas as APIs e verificar resposta HTTP, código de erro quando aplicável e estado persistido. Em toda rejeição, verificar que dados e versões não mudaram.

| ID | Dado / Quando | Então |
| --- | --- | --- |
| CA01 | Gestor cria tarefa em projeto existente, sem responsável | 201; status inicial PENDENTE, versão 1, sem data de conclusão |
| CA02 | Cliente tenta definir outro status na criação | 400; nenhuma tarefa criada |
| CA03 | Gestor ou responsável atual inicia tarefa pendente | 200; EM_ANDAMENTO |
| CA04 | Tarefa em andamento retorna a pendente | 200; PENDENTE |
| CA05 | Tarefa em andamento com responsável ativo é concluída | 200; CONCLUIDA e data UTC preenchida |
| CA06 | Tarefa em andamento sem responsável é concluída pelo Gestor | 409 `RESPONSAVEL_OBRIGATORIO`; mantém estado anterior |
| CA07 | Tarefa pendente tenta ir diretamente a concluída | 409 `TRANSICAO_INVALIDA`, mesmo com responsável |
| CA08 | Gestor reabre tarefa concluída | 200; PENDENTE, responsável preservado e data de conclusão limpa |
| CA09 | Responsável com papel Usuário tenta reabrir tarefa concluída | 403; estado preservado |
| CA10 | Gestor tenta passar de concluída diretamente para andamento | 409 `TRANSICAO_INVALIDA` |
| CA11 | Usuário altera tarefa alheia ou sem responsável | 403, inclusive se informar seu ID no corpo |
| CA12 | Usuário executa qualquer ação exclusiva de Gestor | 403; parametrizar todas as ações da matriz de permissões |
| CA13 | Requisição protegida sem token, com token expirado/adulterado ou conta inativa | 401; nenhum efeito |
| CA14 | Conta ativa faz login correto/incorreto | 200 com token no sucesso; 401 no erro, sem revelar existência da conta |
| CA15 | Gestor exclui projeto com ao menos uma tarefa em andamento | 409 `PROJETO_COM_TAREFA_EM_ANDAMENTO`; tudo preservado |
| CA16 | Gestor exclui projeto vazio ou apenas com tarefas pendentes/concluídas | 204; exclusão lógica atômica; consultas ao projeto/tarefas retornam 404 |
| CA17 | Falha de persistência durante exclusão em conjunto | Rollback completo, sem exclusão parcial |
| CA18 | Gestor exclui tarefa pendente/em andamento/concluída individualmente | 204 para as duas primeiras; 409 `TAREFA_CONCLUIDA` para a última |
| CA19 | Gestor exclui Responsável referenciado, inclusive por tarefa excluída | 409 `REGISTRO_EM_USO`; perfil, conta e vínculos preservados |
| CA20 | Gestor exclui Responsável sem referência | 204; conta e perfil removidos atomicamente |
| CA21 | Gestor desativa Responsável com tarefa visível não concluída | 409 `REGISTRO_EM_USO`; após reatribuição, desativação permitida |
| CA22 | Responsável desativado tinha tarefa concluída | Histórico preservado; login e token anterior rejeitados; nova atribuição rejeitada |
| CA23 | Gestor exclui status adicional livre / referenciado / protegido | 204 / 409 `REGISTRO_EM_USO` / 409 `REGISTRO_PROTEGIDO` |
| CA24 | Gestor renomeia status ou cria status adicional na mesma categoria | Nome não muda as regras; repetir matriz também com status adicionais |
| CA25 | Cliente repete o mesmo StatusId com versão atual | 200; representação e versão inalteradas |
| CA26 | Duas edições usam a mesma versão inicial | Apenas uma grava; outra retorna 409 `CONCORRENCIA` |
| CA27 | Exclusão de projeto disputa com criação/início de tarefa | Resultado serializável: exclusão bloqueada se tarefa em andamento vencer; se exclusão vencer, operação sobre projeto excluído falha; nunca tarefa visível órfã |
| CA28 | Desativação/exclusão de responsável disputa com atribuição | Não permitir nova atribuição a conta removida/inativa nem desativar perfil com tarefa ativa recém-atribuída |
| CA29 | Cliente envia alvo inexistente, referência inválida, corpo inválido ou versão ausente | 404 para alvo; 400 para os demais, após autenticação e autorização aplicáveis |
| CA30 | Cliente repete exclusão de recurso já excluído | 404; nenhum efeito adicional |
| CA31 | Gestor tenta excluir/desativar a própria conta ou remover/rebaixar último Gestor ativo | 409 `REGISTRO_PROTEGIDO` |
| CA32 | Edição genérica tenta alterar status, responsável ou projeto da tarefa | 400; não contorna operações específicas |
| CA33 | Conta/perfil duplicado ou código de status duplicado | 409 `VALOR_DUPLICADO`; sem registros parciais |
| CA34 | Executar CRUD válido de cada entidade com permissões adequadas | Criar, consultar, editar e excluir conforme contratos e restrições acima |

## 6. Equivalência dos testes e comparação arquitetural

- Manter uma suíte de contrato HTTP parametrizada pela URL da API, com os mesmos CA01–CA34 para MVC e Clean Architecture. Expandir tabelas de transições e permissões em casos parametrizados, incluindo as combinações proibidas.
- Executar testes de integração em PostgreSQL real via Docker, com a mesma versão, schema funcional equivalente e dados iniciais determinísticos. Cada implementação recebe banco isolado, reiniciado entre cenários; não executar as duas contra dados mutáveis compartilhados.
- Usar a mesma SPA React e o mesmo contrato de rotas, campos, paginação, status HTTP e erros. Definir OpenAPI comum antes da implementação dos endpoints; trocar apenas a URL base.
- Testar regras unitariamente onde residem: Domain/Application na Clean Architecture e os componentes responsáveis pelas mesmas regras no MVC. Igualdade de comportamento é obrigatória; quantidade e estrutura interna de testes não precisam ser iguais.
- Integração cobre chaves estrangeiras, filtros de exclusão lógica, autenticação, transações e rollback. Testes concorrentes usam conexões independentes e sincronização explícita, sem depender de atrasos arbitrários.
- Congelar dataset, índices, configuração do container, recursos de execução, versões das dependências comuns e perfil de carga para comparação de desempenho. Executar cada API separadamente após o mesmo aquecimento.
- Critério de conclusão funcional: todos os cenários comuns aprovados em ambas as APIs e nenhuma divergência não justificada no contrato observado. Metas de latência e cobertura quantitativa pertencem ao plano experimental e não substituem essa aceitação funcional.

## 7. Referências técnicas e próximos artefatos

A separação entre contas Identity e tokens bearer/JWT está documentada pela Microsoft: [Identity API e tokens próprios](https://devblogs.microsoft.com/dotnet/asp-net-core-updates-in-dotnet-8-preview-7/) e [validação JWT bearer](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0). Para implementação das garantias de gravação, consultar [transações no EF Core](https://learn.microsoft.com/en-us/ef/core/saving/transactions).

