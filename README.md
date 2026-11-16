# finflow-db

Schema e migrations do FinFlow (PostgreSQL 18), aplicadas com DbUp.

## Rodar localmente

```powershell
Copy-Item .env.example .env
docker compose up -d --build --wait
```

Sobe o Postgres (`finflow_local`) e roda o migrator uma vez. A API aponta para ele pela `ConnectionStrings__Default`.

## Testes

```powershell
dotnet test
```

Exige Docker. Aplicam as migrations em um Postgres 18 real e verificam constraints, chaves estrangeiras e idempotência.

## Nova migration

Crie `src/FinFlow.Db.Migrator/Scripts/NNNN_descricao.sql` com o próximo número. Nunca edite um script já aplicado em staging ou produção.
