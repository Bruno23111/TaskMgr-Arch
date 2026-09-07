# CI/CD e coleta de métricas

O pipeline definido em `azure-pipelines.yml` é a referência única de integração contínua para as duas implementações. Ele executa em pull requests e alterações nas branches `develop` e `main` que afetem código ou a configuração do pipeline.

## O que o pipeline executa

1. Instala o SDK .NET 10, restaura e compila as soluções MVC e Clean Architecture em modo `Release`.
2. Procura projetos `*Tests.csproj`. Quando existirem, executa os testes e publica TRX e cobertura Cobertura. Enquanto os testes não existem, a etapa informa isso e não inventa cobertura.
3. Instala Lizard 1.17.10 e executa `scripts/analyze_csharp_metrics.py` nas duas árvores de código C#.
4. Publica os artefatos `taskmgr-metrics` e `taskmgr-apis` no Azure Pipelines.

Não há implantação automática nesta primeira versão: o repositório ainda não contém Dockerfiles das APIs nem ambiente de destino. A publicação das APIs compiladas é a etapa de entrega contínua; uma etapa de deploy será adicionada quando esses elementos existirem.

## Artefato `taskmgr-metrics`

| Arquivo | Conteúdo |
| --- | --- |
| `metrics-summary.json` | Totais por arquitetura, versão do commit, NLOC, complexidade ciclomática, quantidade de funções e índice de manutenibilidade ponderado. |
| `csharp-file-metrics.csv` | Métricas por arquivo. |
| `csharp-method-metrics.csv` | NLOC e complexidade ciclomática por método. |
| `git-history.csv` | Histórico de commits disponível no checkout do build. |

As arquiteturas são identificadas como `MVC` e `CLEAN_ARCH`. O comparativo deve sempre usar builds das duas implementações com o mesmo conjunto de requisitos funcionais, mesma configuração do pipeline e mesmo tipo de agente.

## Definições de métricas

- **NLOC:** linhas físicas não vazias após remover comentários.
- **Complexidade ciclomática:** soma das complexidades dos métodos, calculada por Lizard para C#.
- **Índice de manutenibilidade:** variante normalizada de 0 a 100, sem bônus por comentários: `max(0, (171 - 5.2 ln(V) - 0.23 CC - 16.2 ln(LOC)) × 100 / 171)`. O volume de Halstead `V` é uma aproximação léxica reproduzível, registrada no CSV. O índice só pode ser comparado entre builds deste pipeline; não deve ser comparado diretamente a valores gerados por outra ferramenta ou fórmula.
- **Cobertura:** cobertura Cobertura gerada pelos testes, quando projetos de teste forem adicionados.
- **Tempo de implementação:** derivado posteriormente de `git-history.csv` e dos vínculos de commits/PRs com as work items do Azure DevOps. O timestamp de um commit isolado não representa, por si, esforço humano.

Radon não é utilizado para análise do backend: ele é um analisador de código Python e não mede C# de forma válida. Lizard é usado para a complexidade C#; a fórmula de manutenibilidade e suas limitações são explícitas para manter a análise auditável.

## Como criar e executar o pipeline no Azure DevOps

1. Faça commit e push desta configuração na branch `feature/ci-cd-metrics`.
2. Em Azure DevOps, abra **Pipelines → New pipeline → Azure Repos Git → TaskMgr-Arch → Existing Azure Pipelines YAML file**.
3. Selecione a branch `feature/ci-cd-metrics` e o arquivo `/azure-pipelines.yml`; salve e execute o pipeline.
4. Depois de validar a primeira execução, abra um PR para `develop`. O gatilho de PR passa a validar alterações de código nas duas implementações.
5. Baixe `taskmgr-metrics` em cada build comparativo e mantenha os arquivos brutos junto com a análise do TCC.

## Próximos incrementos necessários

- Criar projetos de testes unitários e de integração para MVC e Clean Architecture, cobrindo CA01–CA34 de `docs/requisitos-funcionais.md`.
- Adicionar Dockerfiles das APIs e uma etapa de deploy para ambiente de homologação quando esse ambiente existir.
- Adicionar benchmarks de carga com dataset, perfil de carga, aquecimento e infraestrutura controlados. Essa etapa deve produzir um artefato separado de latência e throughput; não deve compartilhar recursos com o build de métricas estáticas.
- Definir uma consulta no Azure DevOps que relacione work items, commits e PRs para análise de tempo de implementação.
