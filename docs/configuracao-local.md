# Configuração local

## PostgreSQL

1. Copie `.env.example` para `.env` na raiz do repositório.
2. Troque `POSTGRES_PASSWORD` por uma senha local.
3. Execute `docker compose up -d db-postgres`.

Na primeira inicialização do volume, o container cria os bancos isolados `taskmgr_mvc` e `taskmgr_cleanarch`. O arquivo `.env` é ignorado pelo Git.

Se já existir um volume local criado com a configuração anterior, o script de inicialização não será executado automaticamente. Faça backup dos dados necessários e recrie o volume manualmente antes de usar os novos bancos.

## Connection strings das APIs

Nenhuma senha é armazenada em `appsettings.json`. Em desenvolvimento, configure User Secrets no PowerShell, substituindo `<sua-senha>` pela senha definida no `.env`:

```powershell
dotnet user-secrets set "ConnectionStrings:TaskManager" "Host=localhost;Port=5432;Database=taskmgr_mvc;Username=taskmgr;Password=<sua-senha>" --project src/TaskManager.MVC/TaskManager.MVC.API
dotnet user-secrets set "ConnectionStrings:TaskManager" "Host=localhost;Port=5432;Database=taskmgr_cleanarch;Username=taskmgr;Password=<sua-senha>" --project src/TaskManager.CleanArch/TaskManager.CleanArch.WebAPI
```

Em CI/CD e ambientes hospedados, configure `ConnectionStrings__TaskManager` como variável secreta do ambiente.

## Verificação

Inicie cada API separadamente e acesse `GET /health`. A resposta `200` confirma que a API alcança o banco correspondente. As migrations serão criadas junto com as entidades de domínio.
