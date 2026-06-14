# SQL Server Deployment

CivicSync uses SQL Server through EF Core. Keep one database per department node so the decentralized sync model remains intact.

## Database Layout

| Node | Database |
| --- | --- |
| Home Affairs | `CivicSync_HomeAffairs` |
| SARS | `CivicSync_Sars` |
| Municipality | `CivicSync_Municipality` |

## Local SQL Server

Start a local SQL Server container:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "Your_strong_password123"
.\scripts\start-local-sql-server.ps1
```

Run migrations and seed all three node databases:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "Your_strong_password123"
.\scripts\migrate-all-nodes.ps1
```

## Hosted SQL Server

Use SQL Server or Azure SQL Database for deployment. Set each API node's `ConnectionStrings__CivicSyncNode` separately:

```text
ConnectionStrings__CivicSyncNode=Server=<server>;Database=<database>;User Id=<user>;Password=<secret>;TrustServerCertificate=True
```

For Azure SQL, use:

```text
ConnectionStrings__CivicSyncNode=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Set these per node:

| API node | `Node__DepartmentCode` | Database |
| --- | --- | --- |
| Home Affairs API | `HomeAffairs` | `CivicSync_HomeAffairs` |
| SARS API | `Sars` | `CivicSync_Sars` |
| Municipality API | `Municipality` | `CivicSync_Municipality` |

Do not put hosted SQL passwords, API keys, or node shared secrets in `appsettings.json`.

## Deployment Readiness Check

Before deploying from `main`:

```powershell
npm run build --prefix .\Frontend
dotnet build .\aspnet-core\CivicSync.Abp.slnx
dotnet test .\aspnet-core\CivicSync.Abp.slnx --no-build
```
