# CivicSync

Decentralized ledger where individuals need not to fear for security of their information, with the synced database across all government departments. CivicSync keeps a ledger that is up to date, secure, easier to update, and useful for both departments and citizens.

## ABP Backend

This folder contains the ASP.NET/ABP backend implementation for the CivicSync Ledger project.

## Current Backend Scope

- ABP layered solution structure
- SQL Server persistence with EF Core migrations
- Department nodes and peer configuration
- Citizen records
- Change requests and department approvals
- Approver user catalog
- Ledger entries with hash-chain proof fields
- Sync inbox/outbox flow for peer delivery
- xUnit backend tests

## Frontend Scope

- Next.js frontend with TypeScript
- Client-side portal routes for citizen, department, and admin workspaces
- Configurable node API URLs through `NEXT_PUBLIC_CIVICSYNC_*` environment variables

## Biometric Authentication

CivicSync uses browser WebAuthn/passkeys for real device biometric sign-in in the frontend demo. The browser asks the operating system authenticator, such as Windows Hello, Face ID, or fingerprint, to unlock a passkey. CivicSync never receives raw face or fingerprint images.

The backend issues one-time passkey challenges, stores registered public keys, and verifies login assertions by checking the returned challenge, browser origin, authenticator user-verification flags, and WebAuthn signature. A copied credential ID or forged browser response is rejected unless it can produce a valid signature from the registered authenticator key.

The existing face-camera enrollment remains a prototype citizen verification workflow. For real login, use the passkey buttons on the sign-in page.

## Local Development

### Fresh PC Setup

Install these first:

- .NET 10 SDK
- Node.js 22 LTS or newer
- Docker Desktop with the Linux engine running

Clone the repo, then run:

```powershell
npm install --prefix .\Frontend
$env:CIVICSYNC_SQL_PASSWORD = "Your_strong_password123"
.\scripts\setup-local-demo.ps1
```

This starts SQL Server in Docker, creates all three node databases, runs EF migrations, and seeds demo data.

Use the launch profiles in `aspnet-core/src/CivicSync.Web.Host/Properties/launchSettings.json` to run separate nodes:

- `HomeAffairs` on `http://localhost:5076`
- `Sars` on `http://localhost:5077`
- `Municipality` on `http://localhost:5078`

The checked-in shared secret is development-only. Replace it with environment variables, user secrets, or Azure Key Vault outside the local demo.

## Validation

```powershell
dotnet build .\aspnet-core\CivicSync.Abp.slnx
dotnet test .\aspnet-core\CivicSync.Abp.slnx --no-restore
```

## Database Migration

CivicSync uses one SQL Server database per department node for the local decentralized demo:

- `CivicSync_HomeAffairs`
- `CivicSync_Sars`
- `CivicSync_Municipality`

Start a local SQL Server container:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "Your_strong_password123"
.\scripts\start-local-sql-server.ps1
```

Run all node migrations and seed data:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "Your_strong_password123"
.\scripts\migrate-all-nodes.ps1
```

Run one node migration only:

```powershell
.\scripts\migrate-node.ps1 -Node HomeAffairs
.\scripts\migrate-node.ps1 -Node Sars
.\scripts\migrate-node.ps1 -Node Municipality
```

After migration, confirm in SQL Server Management Studio, Azure Data Studio, or your SQL Server dashboard that the three databases exist and contain seeded `DepartmentNodes`, `Citizens`, and peer configuration.

## Running The Demo

Start each API in a separate terminal:

```powershell
.\scripts\run-node-api.ps1 -Node HomeAffairs
.\scripts\run-node-api.ps1 -Node Sars
.\scripts\run-node-api.ps1 -Node Municipality
```

Or open all three node API terminals at once:

```powershell
.\scripts\run-all-node-apis.ps1
```

Start the frontend:

```powershell
npm run dev --prefix .\Frontend
```

The checked-in shared secret remains development-only. For deployment, set `CIVICSYNC_NODE_SHARED_SECRET` and environment-specific SQL Server connection strings outside source control.

Each node runs automatic background synchronization by default. The local demo publishes pending outbox events and applies pending inbox entries every 15 seconds, while the frontend polls node data every 5 seconds so record, request, ledger, inbox, and outbox views update without pressing Refresh.

## SQL Server Deployment

Hosted deployments use SQL Server or Azure SQL Database instead of local Docker SQL Server. Keep the same one-database-per-node model:

- `CivicSync_HomeAffairs`
- `CivicSync_Sars`
- `CivicSync_Municipality`

Configure each node with its own hosted SQL Server connection string:

```text
ConnectionStrings__CivicSyncNode=Server=<server>;Database=<database>;User Id=<user>;Password=<secret>;TrustServerCertificate=True
```

Full setup details are in [`docs/sql-server-deployment.md`](docs/sql-server-deployment.md).
