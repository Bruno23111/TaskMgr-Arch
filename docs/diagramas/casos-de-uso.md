# Casos de uso — TaskMgr-ArchPerf

Especificação funcional comum às versões MVC e Clean Architecture, baseada na seção 3 do artigo `ARTIGO_CleanArchitecture_vs_MVC_v3-revisado.pdf`. Este diagrama reflete os requisitos funcionais aprovados; não indica funcionalidades já implementadas.

```mermaid
flowchart LR
    usuario["«ator»<br/>Usuário"]
    gestor["«ator»<br/>Gestor"]

    subgraph sistema["TaskMgr-ArchPerf — Casos de uso comuns"]
        direction TB
        autenticar(["Autenticar-se<br/>Identity local / JWT"])
        criarProjeto(["Criar Projeto"])
        listarProjetos(["Listar / Consultar Projetos"])
        editarProjeto(["Editar Projeto"])
        excluirProjeto(["Excluir Projeto e suas Tarefas<br/>Exclusão lógica atômica"])
        adicionarTarefa(["Adicionar Tarefa ao Projeto<br/>Status inicial PENDENTE"])
        listarTarefas(["Listar / Consultar Tarefas"])
        editarTarefa(["Editar Tarefa não concluída"])
        excluirTarefa(["Excluir Tarefa não concluída<br/>Exclusão lógica"])
        atribuirResponsavel(["Atribuir, trocar ou remover Responsável<br/>Tarefa não concluída"])
        alterarStatus(["Alterar Status da Tarefa"])
        reabrir(["Reabrir Tarefa<br/>CONCLUIDA para PENDENTE"])
        papeis(["Alterar papéis das contas"])
        validarCadastros(["Validar vínculos e proteções<br/>de contas e Status"])
        consultarCatalogos(["Consultar Responsáveis e Status"])
        responsaveis(["Gerenciar Responsáveis e contas<br/>Criar, editar, desativar e excluir"])
        status(["Gerenciar catálogo de Status<br/>Criar, consultar, editar e excluir"])
        validarExclusao(["Validar exclusão do Projeto"])
        validarTransicao(["Validar transição de Status"])
        validarConclusao(["Verificar responsável<br/>quando o destino for conclusão"])
        regraExclusao["RN01: impedir exclusão de Projeto<br/>com Tarefas em andamento"]
        regraTransicao["RN02: aceitar apenas<br/>transições permitidas"]
        regraConclusao["RN03: exigir Responsável ativo<br/>antes da conclusão"]
        precondicao["Operações protegidas: conta ativa e autenticada<br/>Usuário: edita e muda status apenas de suas tarefas<br/>Reabertura: exclusiva do Gestor"]
        protecoes["Preservar referências, inclusive de tarefas excluídas<br/>Proteger Status do seed e último Gestor ativo<br/>Desativar Responsável só sem tarefas abertas visíveis"]
    end

    usuario --- autenticar
    gestor --- criarProjeto
    usuario --- listarProjetos
    gestor --- editarProjeto
    gestor --- excluirProjeto
    gestor --- adicionarTarefa
    usuario --- listarTarefas
    usuario --- editarTarefa
    gestor --- excluirTarefa
    gestor --- atribuirResponsavel
    usuario --- alterarStatus
    usuario --- consultarCatalogos
    gestor --- autenticar
    gestor --- listarProjetos
    gestor --- listarTarefas
    gestor --- editarTarefa
    gestor --- alterarStatus
    gestor --- reabrir
    gestor --- papeis
    gestor --- consultarCatalogos
    gestor --- responsaveis
    gestor --- status

    excluirProjeto -.->|"«include»"| validarExclusao
    alterarStatus -.->|"«include»"| validarTransicao
    alterarStatus -.->|"«include»"| validarConclusao
    reabrir -.->|"«include»"| validarTransicao
    responsaveis -.->|"«include»"| validarCadastros
    status -.->|"«include»"| validarCadastros
    papeis -.->|"«include»"| validarCadastros
    validarCadastros -.- protecoes
    validarExclusao -.- regraExclusao
    validarTransicao -.- regraTransicao
    validarConclusao -.- regraConclusao

    classDef ator fill:#e2e8f0,stroke:#334155,color:#0f172a
    classDef nota fill:#fef3c7,stroke:#b45309,color:#78350f
    class usuario,gestor ator
    class regraExclusao,regraTransicao,regraConclusao,precondicao,protecoes nota
```

As linhas contínuas sem seta representam associações do ator. As setas `include` indicam verificações obrigatórias; a verificação de responsável só bloqueia a mudança quando o destino é a conclusão. Autenticação é uma precondição das operações de gestão, sem exigir novo login a cada operação. Os CRUDs de Responsável e Status estão agrupados para facilitar a leitura.

## Regras e critérios de aceitação

As decisões estão em [Requisitos funcionais](../requisitos-funcionais.md), incluindo matriz de transições, permissões, exclusões e CA01–CA34. O Usuário pode editar e alterar o status somente de tarefas atribuídas a ele, respeitando a matriz. Reabertura é exclusiva do Gestor e aparece separadamente para destacar sua permissão; é uma modalidade de alteração de status. As associações do diagrama não dispensam essas condições.

O fluxo principal é PENDENTE → EM_ANDAMENTO → CONCLUIDA. EM_ANDAMENTO pode retornar a PENDENTE. CONCLUIDA só retorna a PENDENTE por reabertura; saltos diretos de PENDENTE para CONCLUIDA ou de CONCLUIDA para EM_ANDAMENTO são proibidos. Mudanças dentro da mesma categoria e repetição do mesmo StatusId seguem as regras de idempotência aprovadas.

A exclusão de projeto sem tarefas em andamento inclui tarefas pendentes e concluídas, atomicamente. É uma exceção à proibição de exclusão individual de tarefa concluída. Referências impedem exclusão de Responsável/Status; tarefas visíveis não concluídas impedem desativação de Responsável. O próprio Gestor não pode excluir ou desativar sua conta. A validação de cadastros aplica as proteções pertinentes a cada operação.

O status inicial é PENDENTE. A conclusão exige responsável ativo. Projetos e tarefas têm exclusão lógica; referências a responsáveis e status são preservadas. Não houve verificação das tasks no Azure DevOps.
