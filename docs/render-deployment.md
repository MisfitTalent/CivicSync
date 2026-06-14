# Render Deployment

This repo includes a Render blueprint for a SQL Server-backed demo shape:

- three Docker web services for CivicSync API nodes
- one Docker web service for the Next.js frontend
- SQL Server connection strings supplied from a hosted SQL Server or Azure SQL Database

Use `render.yaml` for SQL Server-backed services.

## Required Environment Values

Use one shared random value for all node sync secrets:

```text
Node__SharedSecret=<same-secret-on-all-node-services>
Node__Peers__0__SharedSecret=<same-secret-on-all-node-services>
Node__Peers__1__SharedSecret=<same-secret-on-all-node-services>
Security__ApiKey=<same-api-key-used-by-the-frontend>
```

Set these on all three API services:

```text
Cors__AllowedOrigins__0=https://<frontend-service>.onrender.com
Passkey__RelyingPartyId=<frontend-service>.onrender.com
Passkey__AllowedOrigins__0=https://<frontend-service>.onrender.com
```

Set each API service's own public URL:

```text
Node__ApiBaseUrl=https://<this-api-service>.onrender.com
```

Set peer URLs to the other API services:

```text
Node__Peers__0__ApiBaseUrl=https://<peer-api-service>.onrender.com
Node__Peers__1__ApiBaseUrl=https://<peer-api-service>.onrender.com
```

Set one SQL Server connection string per API service:

```text
ConnectionStrings__CivicSyncNode=Server=<server>;Database=<database>;User Id=<user>;Password=<secret>;TrustServerCertificate=True
```

Use:

- `CivicSync_HomeAffairs` for Home Affairs
- `CivicSync_Sars` for SARS
- `CivicSync_Municipality` for Municipality

Set frontend values:

```text
NEXT_PUBLIC_CIVICSYNC_API_KEY=<same-value-as-Security__ApiKey>
NEXT_PUBLIC_CIVICSYNC_HOME_AFFAIRS_API_URL=https://<home-affairs-api>.onrender.com
NEXT_PUBLIC_CIVICSYNC_SARS_API_URL=https://<sars-api>.onrender.com
NEXT_PUBLIC_CIVICSYNC_MUNICIPALITY_API_URL=https://<municipality-api>.onrender.com
```

## Migration

Run the migrator image once per node after database creation and before the demo:

```powershell
dotnet run --project .\aspnet-core\src\CivicSync.Migrator\CivicSync.Migrator.csproj
```

For a hosted database from your workstation, set the node environment variables first, then run:

```powershell
$env:ConnectionStrings__CivicSyncNode = "Server=<server>;Database=CivicSync_HomeAffairs;User Id=<user>;Password=<secret>;TrustServerCertificate=True"
$env:Node__DepartmentCode = "HomeAffairs"
$env:Node__ApiBaseUrl = "https://<home-affairs-api>.onrender.com"
$env:Node__SharedSecret = "<sync-secret>"
$env:Node__Peers__0__DepartmentCode = "Sars"
$env:Node__Peers__0__ApiBaseUrl = "https://<sars-api>.onrender.com"
$env:Node__Peers__0__SharedSecret = "<sync-secret>"
$env:Node__Peers__1__DepartmentCode = "Municipality"
$env:Node__Peers__1__ApiBaseUrl = "https://<municipality-api>.onrender.com"
$env:Node__Peers__1__SharedSecret = "<sync-secret>"
dotnet run --project .\aspnet-core\src\CivicSync.Migrator\CivicSync.Migrator.csproj
```

Repeat with the SARS and Municipality database/department values.
