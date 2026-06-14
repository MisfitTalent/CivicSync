# Azure SQL Deployment

CivicSync uses SQL Server through EF Core. For deployment, do not use the checked-in local `localhost` connection string. Host the node databases in Azure SQL Database and inject connection strings through environment variables or Azure App Service configuration.

## Database Layout

Use one Azure SQL logical server with one database per CivicSync department node:

| Node | Database |
| --- | --- |
| Home Affairs | `CivicSync_HomeAffairs` |
| SARS | `CivicSync_Sars` |
| Municipality | `CivicSync_Municipality` |

This keeps the decentralized demo model intact: every deployed API node has its own SQL database and syncs through the ledger inbox/outbox workflow.

## Provision Azure SQL

Prerequisites:

- Azure CLI installed and signed in with `az login`.
- An Azure subscription selected with `az account set --subscription "<subscription-id-or-name>"`.
- A strong SQL admin password stored outside source control.

Run:

```powershell
$env:CIVICSYNC_AZURE_SQL_ADMIN_PASSWORD = "<strong-password>"

.\scripts\provision-azure-sql.ps1 `
  -ResourceGroup "rg-civicsync-demo" `
  -Location "southafricanorth" `
  -SqlServerName "sql-civicsync-demo" `
  -SqlAdminUser "civicsyncadmin" `
  -AllowAzureServices
```

Use another Azure region if your subscription does not support `southafricanorth`.

## Configure Deployed API Nodes

Each API deployment must set its own database connection string:

```text
ConnectionStrings__CivicSyncNode=Server=tcp:<server>.database.windows.net,1433;Initial Catalog=<database>;Persist Security Info=False;User ID=<user>;Password=<secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Set these per node:

| API node | `Node__DepartmentCode` | `ConnectionStrings__CivicSyncNode` database |
| --- | --- | --- |
| Home Affairs API | `HomeAffairs` | `CivicSync_HomeAffairs` |
| SARS API | `Sars` | `CivicSync_Sars` |
| Municipality API | `Municipality` | `CivicSync_Municipality` |

Also configure:

```text
ASPNETCORE_ENVIRONMENT=Production
Node__ApiBaseUrl=<public-api-url-for-this-node>
Node__SharedSecret=<store-in-secret-or-key-vault>
Security__ApiKey=<store-in-secret-or-key-vault>
Node__Peers__0__DepartmentCode=<peer-code>
Node__Peers__0__ApiBaseUrl=<peer-public-api-url>
Node__Peers__0__SharedSecret=<same-peer-secret>
Node__Peers__1__DepartmentCode=<peer-code>
Node__Peers__1__ApiBaseUrl=<peer-public-api-url>
Node__Peers__1__SharedSecret=<same-peer-secret>
```

Do not put production SQL passwords, API keys, or node shared secrets in `appsettings.json`.

## Run Migrations Against Azure SQL

Run each node migration with SQL login credentials:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "<azure-sql-password>"

.\scripts\migrate-node.ps1 `
  -Node HomeAffairs `
  -SqlServer "tcp:<server>.database.windows.net,1433" `
  -UseSqlLogin `
  -SqlUser "civicsyncadmin"

.\scripts\migrate-node.ps1 `
  -Node Sars `
  -SqlServer "tcp:<server>.database.windows.net,1433" `
  -UseSqlLogin `
  -SqlUser "civicsyncadmin"

.\scripts\migrate-node.ps1 `
  -Node Municipality `
  -SqlServer "tcp:<server>.database.windows.net,1433" `
  -UseSqlLogin `
  -SqlUser "civicsyncadmin"
```

Verify in SQL Server Management Studio or Azure Data Studio that all three databases exist and contain seeded `DepartmentNodes`, `KnownPeerNodes`, `DepartmentUsers`, and demo citizen data.

## Deployment Readiness Check

Before deploying from `main`:

```powershell
npm run build --prefix .\Frontend
dotnet build .\aspnet-core\CivicSync.Abp.slnx
dotnet test .\aspnet-core\CivicSync.Abp.slnx --no-build
```

After deployment, manually confirm:

- Each API node can connect to its own Azure SQL database.
- Each API node can reach peer API URLs.
- A submitted change request appears in the active node database.
- Commit/publish/apply sync updates the peer node databases.
