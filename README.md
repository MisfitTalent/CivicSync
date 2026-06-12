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

## Biometric Authentication

CivicSync uses browser WebAuthn/passkeys for real device biometric sign-in in the frontend demo. The browser asks the operating system authenticator, such as Windows Hello, Face ID, or fingerprint, to unlock a passkey. CivicSync never receives raw face or fingerprint images.

The existing face-camera enrollment remains a prototype citizen verification workflow. For real login, use the passkey buttons on the sign-in page.

## Local Development

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

Run all node migrations and seed data with Windows Authentication:

```powershell
.\scripts\migrate-all-nodes.ps1
```

Run all node migrations against a SQL Server container or SQL login:

```powershell
$env:CIVICSYNC_SQL_PASSWORD = "your-local-sa-password"
.\scripts\migrate-all-nodes.ps1 -SqlServer "localhost,1433" -UseSqlLogin
```

Run one node migration only:

```powershell
.\scripts\migrate-node.ps1 -Node HomeAffairs
.\scripts\migrate-node.ps1 -Node Sars
.\scripts\migrate-node.ps1 -Node Municipality
```

After migration, confirm in SQL Server Management Studio that the three databases exist and contain seeded `DepartmentNodes`, `Citizens`, and peer configuration.

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

