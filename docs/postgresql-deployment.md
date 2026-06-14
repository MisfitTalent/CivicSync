# PostgreSQL Deployment Profile

The main CivicSync submission profile remains SQL Server to match the project pack. This PostgreSQL profile exists for public deployment when SQL Server hosting is not available.

PostgreSQL is enabled by setting:

```text
Database__Provider=PostgreSql
```

The PostgreSQL profile creates the schema from the EF model with `EnsureCreated` in the migrator. The SQL Server profile remains migration-backed.

## Database Layout

Render's free tier allows one active PostgreSQL database per account. CivicSync still runs as three API nodes by using one database with one schema per node.

| Node | Database | Schema/Search Path |
| --- | --- |
| Home Affairs | `civicsync_homeaffairs` | `homeaffairs` |
| SARS | `civicsync_homeaffairs` | `sars` |
| Municipality | `civicsync_homeaffairs` | `municipality` |

## Local PostgreSQL Demo

```powershell
$env:CIVICSYNC_POSTGRES_PASSWORD = "postgres"
.\scripts\start-local-postgres.ps1
.\scripts\migrate-all-nodes-postgres.ps1
```

## Hosted PostgreSQL

Set each API node's connection string:

```text
Database__Provider=PostgreSql
Database__PostgreSqlSchema=<schema>
ConnectionStrings__CivicSyncNode=Host=<host>;Port=5432;Database=civicsync_homeaffairs;Username=<user>;Password=<secret>;Ssl Mode=Require;Trust Server Certificate=true;Search Path=<schema>
```

Set the schema/search path per node:

- Home Affairs: `homeaffairs`
- SARS: `sars`
- Municipality: `municipality`

Run the PostgreSQL migrator command once per node. For Render, pass the external host and `-UseSsl`:

```powershell
$env:CIVICSYNC_POSTGRES_PASSWORD = "<secret>"
.\scripts\migrate-all-nodes-postgres.ps1 `
  -PostgresHost "<host>" `
  -PostgresUser "<user>" `
  -PostgresDatabase "civicsync_homeaffairs" `
  -HomeAffairsApiBaseUrl "https://<home-affairs-api>.onrender.com" `
  -SarsApiBaseUrl "https://<sars-api>.onrender.com" `
  -MunicipalityApiBaseUrl "https://<municipality-api>.onrender.com" `
  -UseSsl
```

Each API service still needs its own `Node__DepartmentCode`, `Node__ApiBaseUrl`, peer settings, and connection string with the matching `Search Path`.
