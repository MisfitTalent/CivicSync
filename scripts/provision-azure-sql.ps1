param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory = $true)]
    [string]$Location,

    [Parameter(Mandatory = $true)]
    [string]$SqlServerName,

    [Parameter(Mandatory = $true)]
    [string]$SqlAdminUser,

    [string]$SqlAdminPassword = $env:CIVICSYNC_AZURE_SQL_ADMIN_PASSWORD,

    [string]$ServiceObjective = "Basic",

    [switch]$AllowAzureServices
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($SqlAdminPassword)) {
    throw "Azure SQL admin password missing. Set CIVICSYNC_AZURE_SQL_ADMIN_PASSWORD or pass -SqlAdminPassword."
}

$databases = @(
    "CivicSync_HomeAffairs",
    "CivicSync_Sars",
    "CivicSync_Municipality"
)

az account show | Out-Null

az group create `
    --name $ResourceGroup `
    --location $Location | Out-Null

az sql server create `
    --resource-group $ResourceGroup `
    --name $SqlServerName `
    --location $Location `
    --admin-user $SqlAdminUser `
    --admin-password $SqlAdminPassword | Out-Null

if ($AllowAzureServices) {
    az sql server firewall-rule create `
        --resource-group $ResourceGroup `
        --server $SqlServerName `
        --name AllowAzureServices `
        --start-ip-address 0.0.0.0 `
        --end-ip-address 0.0.0.0 | Out-Null
}

foreach ($database in $databases) {
    az sql db create `
        --resource-group $ResourceGroup `
        --server $SqlServerName `
        --name $database `
        --service-objective $ServiceObjective | Out-Null
}

$serverHost = "$SqlServerName.database.windows.net"

Write-Host "Azure SQL databases provisioned on $serverHost"
Write-Host ""
Write-Host "Set one connection string per deployed CivicSync API node:"
foreach ($database in $databases) {
    Write-Host "ConnectionStrings__CivicSyncNode=Server=tcp:$serverHost,1433;Initial Catalog=$database;Persist Security Info=False;User ID=$SqlAdminUser;Password=<store-in-secret>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}
