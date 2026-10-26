# CI/CD e coleta de métricas

O workflow `.github/workflows/ci.yml` executa a integração contínua das duas implementações. Ele roda em todos os pushes e pull requests direcionados a `develop` e `main` e também pode ser iniciado manualmente pela aba **Actions** do GitHub. O repositório GitHub passa a ser a origem dos pushes e pull requests que precisam de validação automática.

O workflow seleciona Ubuntu 24.04, .NET SDK 10.0.401 e Python 3.13 em um runner hospedado pelo GitHub. O arquivo `global.json` impede seleção silenciosa de outro SDK, e o checkout com `fetch-depth: 0` disponibiliza o histórico alcançável completo para a coleta Git. O computador local pode ficar desligado durante o CI.

## O que o pipeline executa

1. Obtém o código com histórico Git completo, instala o SDK .NET 10.0.401 e compila as soluções MVC e Clean Architecture em modo `Release`.
2. Procura projetos `*Tests.csproj` em `src/` e `tests/`. Quando existirem, restaura e executa todos os testes, salva TRX e cobertura Cobertura e falha se algum teste falhar. Se não houver projetos, emite um aviso explícito e não inventa cobertura.
3. Instala Lizard 1.17.10 e executa `scripts/analyze_csharp_metrics.py` nas duas árvores de código C#.
4. Salva os artefatos `taskmgr-test-results`, `taskmgr-metrics` e `taskmgr-apis` no GitHub Actions por 30 dias.

Não há implantação automática nesta primeira versão: o repositório ainda não contém Dockerfiles das APIs nem ambiente de destino. A publicação das APIs compiladas é a etapa de entrega contínua; uma etapa de deploy será adicionada quando esses elementos existirem.

## Evidências no GitHub Actions

Os artefatos ficam disponíveis na página de cada execução do workflow, na aba **Actions**. O envio ao Azure Blob Storage não faz parte deste workflow porque a assinatura Azure de estudante está inativa. Para preservar as evidências do TCC além dos 30 dias de retenção configurados, baixe os artefatos dos builds usados na análise.

O repositório privado usa a franquia do plano GitHub Free: 2.000 minutos mensais de runners padrão hospedados e 500 MB de armazenamento de artefatos. Monitore o consumo em **Settings → Billing and licensing** para permanecer na franquia.

`metrics-summary.json` registra o ID da execução e o commit. O histórico Git e os CSVs ficam no artefato `taskmgr-metrics`.

## Backup do código no Azure Repos

O workflow `.github/workflows/backup-azure.yml` copia todas as branches e tags do GitHub para o repositório Azure Repos `TaskMgr-Arch` após pushes e em uma verificação diária. As cópias ficam em `github/<nome-da-branch>` e `github/<nome-da-tag>` no Azure. A branch padrão do repositório Azure é `github/develop`, para mostrar a cópia atual ao abrir o repositório. O workflow não apaga refs antigas nem usa force push; se uma cópia divergir, ele falha e exige reconciliação explícita. As branches históricas `develop` e `main` do Azure permanecem preservadas.

O backup usa o secret de Actions `AZURE_REPOS_BACKUP_PAT`, configurado em 26/10/2026 com um PAT da organização `bruno23111399` e escopo **Code (read and write)**. O token não é gravado no repositório. Após renovar o PAT e atualizar o secret, execute **Backup no Azure Repos** manualmente na aba **Actions** e confira a branch `github/develop` no Azure Repos. Renove o PAT antes de vencer.

## Artefato `taskmgr-metrics`

| Arquivo | Conteúdo |
| --- | --- |
| `metrics-summary.json` | Totais por arquitetura, versão do commit, ambiente, SDK, NLOC, complexidade ciclomática, quantidade de funções e índice de manutenibilidade ponderado. |
| `csharp-file-metrics.csv` | Métricas por arquivo. |
| `csharp-method-metrics.csv` | NLOC e complexidade ciclomática por método. |
| `csharp-test-file-metrics.csv` | Métricas dos arquivos de teste, separadas do código de produção. |
| `csharp-test-method-metrics.csv` | Métricas dos métodos de teste, separadas do código de produção. |
| `git-history.csv` | Histórico de commits disponível no checkout do build. |

As arquiteturas são identificadas como `MVC` e `CLEAN_ARCH`. Os totais em `architectures` no `metrics-summary.json` consideram somente código de produção; métricas de teste ficam em `test_code`. Projetos de teste podem ficar sob a árvore da arquitetura ou nas raízes convencionadas `tests/TaskManager.MVC` e `tests/TaskManager.CleanArch`. O comparativo deve sempre usar builds das duas implementações com o mesmo conjunto de requisitos funcionais, mesma configuração do pipeline e mesmo tipo de agente.

## Definições de métricas

- **NLOC:** linhas físicas não vazias após remover comentários.
- **Complexidade ciclomática:** soma das complexidades dos métodos, calculada por Lizard para C#.
- **Índice de manutenibilidade:** variante normalizada de 0 a 100, sem bônus por comentários: `max(0, (171 - 5.2 ln(V) - 0.23 CC - 16.2 ln(LOC)) × 100 / 171)`. O volume de Halstead `V` é uma aproximação léxica reproduzível, registrada no CSV. O índice só pode ser comparado entre builds deste pipeline; não deve ser comparado diretamente a valores gerados por outra ferramenta ou fórmula.
- **Cobertura:** cobertura Cobertura gerada pelos projetos de teste encontrados.
- **Tempo de implementação:** derivado posteriormente de `git-history.csv` e dos vínculos de commits/PRs com as work items do Azure DevOps. O timestamp de um commit isolado não representa, por si, esforço humano.

Radon não é utilizado para análise do backend: ele é um analisador de código Python e não mede C# de forma válida. Lizard é usado para a complexidade C#; a fórmula de manutenibilidade e suas limitações são explícitas para manter a análise auditável.

## Gatilhos, proteção de branches e migração

- Pushes e pull requests em `develop` e `main` usam os gatilhos do workflow GitHub.
- No GitHub Free com repositório privado, a proteção de branches com checks obrigatórios não está disponível. Confira o resultado do job **Build, testes e métricas** antes de cada merge. Se o plano ou a visibilidade do repositório mudar, configure esse job como check obrigatório.
- O `trigger` do `azure-pipelines.yml` foi desabilitado. Remova também as antigas build policies de `develop` e `main` no Azure Repos: essas policies continuam disparando o pipeline independentemente do gatilho YAML.
- O workflow usa somente a permissão `contents: read` do `GITHUB_TOKEN`. Não precisa do antigo Variable Group `taskmgr-github-mirror` nem de um token para espelhar branches.
- Work items do Azure Boards podem continuar sendo usados; os commits e pull requests validados passam a estar no GitHub. Revise os vínculos usados na análise de tempo de implementação.

## Como reproduzir a coleta

1. Use um commit em `develop` ou `main` no qual MVC e Clean Architecture implementem o mesmo conjunto de requisitos.
2. Confirme no log o runner Ubuntu 24.04, o SDK 10.0.401, Python 3.13 e Lizard 1.17.10.
3. Confirme que build e testes aplicáveis foram aprovados.
4. Baixe `taskmgr-metrics` e registre o ID do build e o commit presentes em `metrics-summary.json`.
5. Preserve os CSVs brutos junto com a análise do TCC; não combine resultados produzidos com versões diferentes do ambiente.

## Próximos incrementos necessários

- Concluir a User Story #78 com testes unitários, integração, contrato e concorrência para MVC e Clean Architecture, cobrindo CA01–CA34 de `docs/requisitos-funcionais.md`.
- Adicionar Dockerfiles das APIs e uma etapa de deploy para ambiente de homologação quando esse ambiente existir.
- Adicionar benchmarks de carga com dataset, perfil de carga, aquecimento e infraestrutura controlados. Essa etapa deve produzir um artefato separado de latência e throughput; não deve compartilhar recursos com o build de métricas estáticas.
- Definir uma consulta que relacione work items do Azure Boards aos commits e PRs do GitHub para análise de tempo de implementação.
