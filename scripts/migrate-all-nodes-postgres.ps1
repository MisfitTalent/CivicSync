param(
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

$nodes = @("HomeAffairs", "Sars", "Municipality")

foreach ($node in $nodes) {
    $args = @(
        "-ExecutionPolicy", "Bypass",
        "-File", ".\scripts\migrate-node-postgres.ps1",
        "-Node", $node,
        "-PostgresHost", $PostgresHost,
        "-PostgresPort", $PostgresPort,
        "-PostgresUser", $PostgresUser,
        "-PostgresDatabase", $PostgresDatabase,
        "-HomeAffairsApiBaseUrl", $HomeAffairsApiBaseUrl,
        "-SarsApiBaseUrl", $SarsApiBaseUrl,
        "-MunicipalityApiBaseUrl", $MunicipalityApiBaseUrl
    )

    if (-not [string]::IsNullOrWhiteSpace($PostgresPassword)) {
        $args += @("-PostgresPassword", $PostgresPassword)
    }

    if ($UseSsl) {
        $args += "-UseSsl"
    }

    & powershell @args
    if ($LASTEXITCODE -ne 0) {
        throw "PostgreSQL migration failed for $node."
    }
}
