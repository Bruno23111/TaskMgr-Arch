# TaskMgr-ArchPerf

Repositório central do Trabalho de Conclusão de Curso (TCC) focado na análise comparativa prática entre a **Clean Architecture** e o padrão **MVC**.

O projeto consiste no desenvolvimento de um sistema gerenciador de tarefas (Task Manager) com operações de CRUD e regras de negócio complexas. O objetivo é mensurar, através de automação em CI/CD e coleta de dados via Azure DevOps, a complexidade ciclomática, manutenibilidade e a fricção gerada pelo desenvolvimento assistido por Inteligência Artificial (IA) em diferentes padrões arquiteturais.

## Tecnologias Utilizadas
* **Backend:** .NET 10 (C#)
* **Frontend:** React (SPA)
* **Banco de Dados:** PostgreSQL
* **Infraestrutura:** Docker e Docker Compose
* **Gestão e CI/CD:** Azure DevOps e Python (Radon)

## Estrutura do Repositório
* `/docs`: Documentações adicionais e scripts analíticos.
* `/infra`: Configurações de infraestrutura local e volumes.
* `/src/TaskManager.MVC`: Protótipo da API construída com padrão MVC (Monolito).
* `/src/TaskManager.CleanArch`: Protótipo da API dividida em camadas (Domain, Application, Infrastructure, WebAPI).

## Como executar o projeto localmente

### 1. Subindo a infraestrutura (Banco de Dados)
Certifique-se de ter o [Docker](https://www.docker.com/) instalado. Na raiz do repositório, execute o comando abaixo para iniciar o container do banco na porta `5432`:

```bash
docker-compose up -d db-postgres