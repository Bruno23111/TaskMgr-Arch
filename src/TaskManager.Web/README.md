# TaskManager.Web

SPA React única para as duas APIs (MVC e Clean Architecture), seguindo o contrato `docs/openapi/taskmgr-v1.yaml`.

## Executar

```powershell
npm install
npm run dev
```

O Vite encaminha `/api/*` para a API escolhida por `VITE_API_URL` (padrão: Clean Architecture em `http://localhost:5278`). Para usar a API MVC, crie `.env.local`:

```
VITE_API_URL=http://localhost:5129
```

A API precisa estar rodando com o perfil `http` e com o Gestor inicial configurado (ver `docs/configuracao-local.md`).

## Scripts

- `npm run dev`: servidor de desenvolvimento
- `npm test`: testes da matriz de transições e permissões (`src/regras.test.ts`)
- `npm run build`: checagem de tipos e build de produção

## Estrutura

- `src/api.ts`: cliente HTTP com injeção do JWT, tratamento de `problem+json` e mensagens de erro
- `src/auth.tsx`: `AuthContext`, sessão no `localStorage`, logout na expiração ou em 401, e `RotaPrivada`
- `src/regras.ts`: matriz de transições e permissões usadas para orientar a interface (a API continua validando tudo)
- `src/pages/`: Login, Projetos (paginação e filtros) e Tarefas (CRUD, status e responsável)
