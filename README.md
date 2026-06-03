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

## Local Development

Use the launch profiles in `src/CivicSync.HttpApi.Host/Properties/launchSettings.json` to run separate nodes:

- `HomeAffairs` on `http://localhost:5076`
- `Sars` on `http://localhost:5077`
- `Municipality` on `http://localhost:5078`

The checked-in shared secret is development-only. Replace it with environment variables, user secrets, or Azure Key Vault outside the local demo.

## Validation

```powershell
dotnet build .\CivicSync.Abp.slnx
dotnet test .\CivicSync.Abp.slnx --no-restore
```
