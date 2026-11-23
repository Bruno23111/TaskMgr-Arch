# Testes de sistema/interface com Selenium

Fluxos ponta a ponta da SPA React (`src/TaskManager.Web`) sobre a API Clean Architecture, com Selenium WebDriver 4 e xUnit.

| Teste | Fluxo | Caso (Apêndice E) |
| --- | --- | --- |
| `SE01_GestorEntraVeOsProjetosESai` | Login do Gestor, lista de projetos, recarregar e sair | CT-01 |
| `SE02_LoginInvalidoMostraMensagemPadraoENaoCriaSessao` | Senha errada e e-mail inexistente (2 dados) | CT-02 |
| `SE03_GestorCriaEEditaProjeto` | Criar, editar e recarregar projeto | CT-03 |
| `SE04_CicloCompletoDaTarefa` | Criar tarefa (Pendente), atribuir, iniciar, concluir e reabrir | CT-07, CT-14, CT-12 |
| `SE05_ConcluirSemResponsavelMostraAlertaEMantemOStatus` | Conclusão sem responsável é recusada | CT-11 |
| `SE06_ExclusaoDeProjetoComTarefaEmAndamentoEhBloqueada` | Exclusão de projeto bloqueada | CT-05 |
| `SE07_UsuarioNaoVeAcoesExclusivasDoGestor` | Papel Usuário sem ações de Gestor | CT-21 |

Cada teste cria os próprios dados pela API (nomes com sufixo aleatório), usa esperas explícitas (`WebDriverWait`) e localizadores estáveis (`name`, `aria-label`, `role=alert`), e salva capturas de tela em cada etapa e na falha.

> O `SE04` espera que a tarefa nasça em **Pendente**: depende da correção do DEF-002 (PR do branch `fix/status-inicial-pendente`).

## Como executar

1. Suba o PostgreSQL, a API Clean Architecture (`http://localhost:5278`) com o Gestor inicial configurado e a SPA (`npm run dev` em `src/TaskManager.Web`, `http://localhost:5173`) — ver `docs/configuracao-local.md`.
2. Execute:

```powershell
$env:TASKMGR_GESTOR_EMAIL = "gestor@taskmgr.test"   # e-mail do Gestor inicial
$env:TASKMGR_GESTOR_SENHA = "<senha do Gestor inicial>"
dotnet test tests/TaskManager.E2E.Selenium
```

| Variável | Padrão | Uso |
| --- | --- | --- |
| `TASKMGR_SPA_URL` / `TASKMGR_API_URL` | `http://localhost:5173` / `http://localhost:5278` | Endereços |
| `TASKMGR_NAVEGADOR` | `edge` | `edge`, `chrome` ou `firefox` |
| `TASKMGR_SEM_JANELA` | `true` | `false` abre o navegador visível |
| `TASKMGR_DRIVER_DIR` | — | Pasta do driver; sem ela, o Selenium Manager baixa o driver |
| `TASKMGR_NAVEGADOR_BIN` | — | Executável do Chrome, quando não for o instalado no sistema |
| `TASKMGR_EVIDENCIAS` | `bin/.../evidencias` | Pasta das capturas de tela |

O projeto não termina em `Tests.csproj` de propósito: o pipeline executa apenas os projetos `*Tests.csproj`, e estes testes exigem a SPA e a API em execução.
