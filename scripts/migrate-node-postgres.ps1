param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("HomeAffairs", "Sars", "Municipality")]
    [string]$Node,

    [string]$PostgresHost = "localhost",

    [int]$PostgresPort = 5432,

    [string]$PostgresUser = "postgres",

    [string]$PostgresPassword = $env:CIVICSYNC_POSTGRES_PASSWORD,

    [string]$PostgresDatabase = "civicsync_homeaffairs",

    [switch]$UseSsl,

    [string]$HomeAffairsApiBaseUrl = "http://localhost:5076",

    [string]$SarsApiBaseUrl = "http://localhost:5077",

    [string]$MunicipalityApiBaseUrl = "http://localhost:5078"
)

$ErrorActionPreference = "Stop"

$nodeConfigs = @{
    HomeAffairs = @{
        Schema = "homeaffairs"
        DepartmentCode = "1"
        ApiBaseUrl = $HomeAffairsApiBaseUrl
        Peers = @(
            @{ DepartmentCode = "2"; ApiBaseUrl = $SarsApiBaseUrl },
            @{ DepartmentCode = "3"; ApiBaseUrl = $MunicipalityApiBaseUrl }
        )
    }
    Sars = @{
        Schema = "sars"
        DepartmentCode = "2"
        ApiBaseUrl = $SarsApiBaseUrl
        Peers = @(
            @{ DepartmentCode = "1"; ApiBaseUrl = $HomeAffairsApiBaseUrl },
            @{ DepartmentCode = "3"; ApiBaseUrl = $MunicipalityApiBaseUrl }
        )
    }
    Municipality = @{
        Schema = "municipality"
        DepartmentCode = "3"
        ApiBaseUrl = $MunicipalityApiBaseUrl
        Peers = @(
            @{ DepartmentCode = "1"; ApiBaseUrl = $HomeAffairsApiBaseUrl },
            @{ DepartmentCode = "2"; ApiBaseUrl = $SarsApiBaseUrl }
        )
    }
}

$config = $nodeConfigs[$Node]

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    throw "PostgreSQL password missing. Set CIVICSYNC_POSTGRES_PASSWORD or pass -PostgresPassword."
}

$env:Database__Provider = "PostgreSql"
$sslOptions = if ($UseSsl) { ";Ssl Mode=Require;Trust Server Certificate=true" } else { "" }
$env:ConnectionStrings__CivicSyncNode = "Host=$PostgresHost;Port=$PostgresPort;Database=$PostgresDatabase;Username=$PostgresUser;Password=$PostgresPassword;Search Path=$($config.Schema)$sslOptions"
$env:Database__PostgreSqlSchema = $config.Schema
$env:Node__DepartmentCode = $config.DepartmentCode
$env:Node__ApiBaseUrl = $config.ApiBaseUrl
$env:Node__SharedSecret = if ([string]::IsNullOrWhiteSpace($env:CIVICSYNC_NODE_SHARED_SECRET)) { "development-node-sync-secret" } else { $env:CIVICSYNC_NODE_SHARED_SECRET }
$env:Node__MaxSyncPublishAttempts = "3"

for ($index = 0; $index -lt $config.Peers.Count; $index++) {
    $peer = $config.Peers[$index]
    Set-Item -Path "Env:Node__Peers__$index__DepartmentCode" -Value $peer.DepartmentCode
    Set-Item -Path "Env:Node__Peers__$index__ApiBaseUrl" -Value $peer.ApiBaseUrl
    Set-Item -Path "Env:Node__Peers__$index__SharedSecret" -Value $env:Node__SharedSecret
}

Write-Host "Creating/migrating $Node node PostgreSQL schema '$($config.Schema)' in database '$PostgresDatabase' on '$($PostgresHost):$PostgresPort'..."
dotnet run --no-build --project .\aspnet-core\src\CivicSync.Migrator\CivicSync.Migrator.csproj
