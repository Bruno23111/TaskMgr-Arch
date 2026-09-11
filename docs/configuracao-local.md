# Configuração local

## PostgreSQL

1. Copie `.env.example` para `.env` na raiz do repositório.
2. Troque `POSTGRES_PASSWORD` por uma senha local.
3. Execute `docker compose up -d db-postgres`.

Na primeira inicialização do volume, o container cria os bancos isolados `taskmgr_mvc` e `taskmgr_cleanarch`. O arquivo `.env` é ignorado pelo Git.

### Volume PostgreSQL já existente

Os scripts em `docker-entrypoint-initdb.d` só são executados quando o volume PostgreSQL é criado. Portanto, se o container ou o volume já existia antes da inclusão dos bancos isolados, eles não serão criados automaticamente.

Se o health check retornar `database "taskmgr_cleanarch" does not exist`, crie somente o banco que falta, sem apagar o volume:

```powershell
docker exec -it taskmgr-postgres psql -U taskmgr -d taskmgr -c "CREATE DATABASE taskmgr_cleanarch;"
```

Para conferir os bancos disponíveis:

```powershell
docker exec -it taskmgr-postgres psql -U taskmgr -d taskmgr -c "\l"
```

Caso seja necessário recriar o volume, faça antes um backup dos dados que deseja preservar.

## Connection strings das APIs

Nenhuma senha é armazenada em `appsettings.json`. Em desenvolvimento, configure User Secrets no PowerShell, substituindo `<sua-senha>` pela senha definida no `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:TaskManager" "Host=localhost;Port=5432;Database=taskmgr_mvc;Username=taskmgr;Password=<sua-senha>" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "ConnectionStrings:TaskManager" "Host=localhost;Port=5432;Database=taskmgr_cleanarch;Username=taskmgr;Password=<sua-senha>" --project src/TaskManager.CleanArch/TaskManager.CleanArch.WebAPI
```

Em CI/CD e ambientes hospedados, configure `ConnectionStrings__TaskManager` como variável secreta do ambiente.

## Verificação

Inicie cada API separadamente e acesse `GET /health`. A resposta `200` confirma que a API alcança o banco correspondente. As migrations serão criadas junto com as entidades de domínio.
