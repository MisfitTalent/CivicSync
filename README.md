# CivicSync Ledger

CivicSync Ledger is a decentralized public-sector citizen record platform for the South African government context. It models separate department nodes such as Home Affairs, SARS, and Municipality as independently hosted services that keep local records, approve citizen updates, write auditable ledger entries, and synchronize accepted changes across peers.

The project is a graduate MVP, not an enterprise production system. It is built to demonstrate sound full-stack architecture, department-scoped data access, biometric login, ledger-backed change approval, and multi-node synchronization.

## ABP Backend

This folder contains the ASP.NET/ABP backend implementation for the CivicSync Ledger project.

## Current Backend Scope

- ABP layered solution structure
- SQL Server persistence with EF Core migrations for the official/local profile
- PostgreSQL deployment profile for free Render hosting
- Department nodes and peer configuration
- Citizen records
- Change requests and department approvals
- Approver user catalog
- Ledger entries with hash-chain proof fields
- Sync inbox/outbox flow for peer delivery
- Background synchronization workers
- Admin operations for department users and node sync controls
- xUnit backend tests

## Frontend Scope

- Next.js frontend with TypeScript
- Client-side portal routes for citizen, department, and admin workspaces
- Configurable node API URLs through `NEXT_PUBLIC_CIVICSYNC_*` environment variables
- Citizen registration, password login, passkey login, and face login
- Face enrollment during registration and from the citizen portal
- Citizen update wizard with multi-field request support
- Live polling for records, requests, ledger, inbox, outbox, and receipts

## Biometric Authentication

CivicSync supports two biometric login paths in the demo:

- Face login uses the browser camera with FaceAPI TinyFaceDetector, 68-point landmarks, and a 128D face recognition descriptor. The backend stores the compact descriptor reference and verifies later camera captures against the enrolled citizen record.
- Passkey login uses browser WebAuthn. The browser asks the operating system authenticator, such as Windows Hello, Face ID, or fingerprint, to unlock a passkey. CivicSync stores registered public keys and verifies login assertions by checking the challenge, origin, user-verification flags, and WebAuthn signature.

CivicSync does not store raw face images. Face login remains an MVP biometric implementation suitable for demonstration, while WebAuthn/passkeys model the stronger production-grade device-authenticator path.

When a citizen email address is changed through an approved request, the frontend reconciles the login account with the Home Affairs citizen record so the new email becomes the login email while keeping the same citizen data and biometric link.

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
npm run build --prefix .\Frontend
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

## Deployment Profiles

### SQL Server Profile

SQL Server is the official project-pack database profile. Hosted SQL Server or Azure SQL Database can be used instead of local Docker SQL Server. Keep the same one-database-per-node model:

- `CivicSync_HomeAffairs`
- `CivicSync_Sars`
- `CivicSync_Municipality`

Configure each node with its own hosted SQL Server connection string:

```text
ConnectionStrings__CivicSyncNode=Server=<server>;Database=<database>;User Id=<user>;Password=<secret>;TrustServerCertificate=True
```

Full setup details are in [`docs/sql-server-deployment.md`](docs/sql-server-deployment.md).

### PostgreSQL Render Profile

The deployed Render demo uses PostgreSQL because free hosted SQL Server was not available. Enable the PostgreSQL provider with:

```text
Database__Provider=PostgreSql
```

The Render/free-tier PostgreSQL shape uses one hosted database with three schemas: `homeaffairs`, `sars`, and `municipality`. This preserves the three-node separation at schema level for the public hosted demo.

Full setup details are in [`docs/postgresql-deployment.md`](docs/postgresql-deployment.md).

## Project Trade-Offs

- The deployed demo uses PostgreSQL for hosting cost reasons, while SQL Server remains supported for the required project-stack profile.
- The multi-node model runs as three API services with separate node configuration and storage boundaries.
- Face authentication stores descriptors rather than raw images and is intended as an MVP biometric workflow.
- POPIA-style field visibility is represented through department-scoped views and redacted fields, not a complete legal policy engine.
- The sync flow is automatic and demo-friendly, but a production system would need stronger retry, monitoring, key management, and operational controls.
  
## Design & Domain Model

- Figma design reference: [CivicSync UI design](https://www.figma.com/community/file/1646141375190685553)
- Domain model diagram: [CivicSync domain model](https://app.diagrams.net/#G1fN67ozym_2Yo3cZqjfRPrE5Dfv-G3IhX)

## AI Usage Disclosure

AI assistance was used for scaffolding, implementation support, debugging, frontend iteration, deployment troubleshooting, and documentation. Final implementation decisions, project scope, architecture, trade-offs, and demo behavior remain explainable from the source code and this README.
