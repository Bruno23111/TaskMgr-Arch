# CI/CD e coleta de métricas

O pipeline definido em `azure-pipelines.yml` é a referência única de integração contínua para as duas implementações. O gatilho CI executa após alterações nas branches `develop` e `main` que afetem código, contrato OpenAPI, requisitos funcionais, scripts, infraestrutura ou a configuração do pipeline. Em Azure Repos, a validação de pull requests é acionada pelas branch policies de `develop` e `main`.

O ambiente de coleta está fixado em Ubuntu 24.04 e .NET SDK 10.0.100. O arquivo `global.json` impede seleção silenciosa de outro SDK, e o checkout com `fetchDepth: 0` disponibiliza o histórico alcançável completo para a coleta Git.

## O que o pipeline executa

1. Obtém o código com histórico Git completo, instala o SDK .NET 10.0.100 e compila as soluções MVC e Clean Architecture em modo `Release`.
2. Procura projetos `*Tests.csproj` em `src/` e `tests/`. Quando existirem, restaura e executa todos os testes, publica TRX e cobertura Cobertura e bloqueia o pipeline se algum teste falhar. Enquanto a User Story #78 não adicionar os projetos, a etapa emite um aviso explícito e não inventa cobertura.
3. Instala Lizard 1.17.10 e executa `scripts/analyze_csharp_metrics.py` nas duas árvores de código C#.
4. Publica os artefatos `taskmgr-metrics` e `taskmgr-apis` no Azure Pipelines.

Não há implantação automática nesta primeira versão: o repositório ainda não contém Dockerfiles das APIs nem ambiente de destino. A publicação das APIs compiladas é a etapa de entrega contínua; uma etapa de deploy será adicionada quando esses elementos existirem.

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
- **Cobertura:** cobertura Cobertura gerada pelos testes, quando projetos de teste forem adicionados.
- **Tempo de implementação:** derivado posteriormente de `git-history.csv` e dos vínculos de commits/PRs com as work items do Azure DevOps. O timestamp de um commit isolado não representa, por si, esforço humano.

Radon não é utilizado para análise do backend: ele é um analisador de código Python e não mede C# de forma válida. Lizard é usado para a complexidade C#; a fórmula de manutenibilidade e suas limitações são explícitas para manter a análise auditável.

## Gatilhos, policies e segurança

- Pushes em `develop` e `main` usam o gatilho `trigger` do YAML.
- Pull requests são validados pelas branch policies do Azure Repos. O bloco YAML `pr` não é usado porque esse recurso não controla PRs hospedados no Azure Repos.
- As policies de build são obrigatórias; build ou teste com falha impede o merge.
- O Variable Group `taskmgr-github-mirror` é declarado somente no estágio `MirrorToGitHub`. Esse estágio não executa em pull requests e só recebe o token depois de uma validação bem-sucedida em `develop` ou `main`.
- O espelhamento usa a credencial apenas durante o push e remove o arquivo temporário ao encerrar a etapa.

## Como reproduzir a coleta

1. Use um commit em `develop` ou `main` no qual MVC e Clean Architecture implementem o mesmo conjunto de requisitos.
2. Confirme no log o agente Ubuntu 24.04, o SDK 10.0.100 e a versão 1.17.10 do Lizard.
3. Confirme que build e testes aplicáveis foram aprovados.
4. Baixe `taskmgr-metrics` e registre o ID do build e o commit presentes em `metrics-summary.json`.
5. Preserve os CSVs brutos junto com a análise do TCC; não combine resultados produzidos com versões diferentes do ambiente.

## Próximos incrementos necessários

- Concluir a User Story #78 com testes unitários, integração, contrato e concorrência para MVC e Clean Architecture, cobrindo CA01–CA34 de `docs/requisitos-funcionais.md`.
- Adicionar Dockerfiles das APIs e uma etapa de deploy para ambiente de homologação quando esse ambiente existir.
- Adicionar benchmarks de carga com dataset, perfil de carga, aquecimento e infraestrutura controlados. Essa etapa deve produzir um artefato separado de latência e throughput; não deve compartilhar recursos com o build de métricas estáticas.
- Definir uma consulta no Azure DevOps que relacione work items, commits e PRs para análise de tempo de implementação.
