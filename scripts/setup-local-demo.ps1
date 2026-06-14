param(
    [string]$SqlPassword = $env:CIVICSYNC_SQL_PASSWORD
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
    $SqlPassword = "Your_strong_password123"
    Write-Warning "CIVICSYNC_SQL_PASSWORD is not set. Using development password 'Your_strong_password123'."
}

$env:CIVICSYNC_SQL_PASSWORD = $SqlPassword

Write-Host "Starting CivicSync SQL Server..."
.\scripts\start-local-sql-server.ps1 -SqlPassword $SqlPassword

Write-Host "Migrating and seeding CivicSync node databases..."
.\scripts\migrate-all-nodes.ps1 -SqlPassword $SqlPassword

Write-Host "CivicSync local demo is ready."
Write-Host "Run APIs:"
Write-Host "  .\scripts\run-all-node-apis.ps1"
Write-Host "Run frontend:"
Write-Host "  npm run dev --prefix .\Frontend"
