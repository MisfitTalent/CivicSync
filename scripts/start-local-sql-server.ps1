param(
    [string]$ContainerName = "civicsync-sqlserver",

    [int]$SqlPort = 1433,

    [string]$SqlPassword = $env:CIVICSYNC_SQL_PASSWORD
)

$ErrorActionPreference = "Stop"

docker info | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw "Docker is not running. Start Docker Desktop with the Linux engine enabled, then run this script again."
}

if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
    $SqlPassword = "Your_strong_password123"
    Write-Warning "CIVICSYNC_SQL_PASSWORD is not set. Using development password 'Your_strong_password123'."
}

$env:CIVICSYNC_SQL_PASSWORD = $SqlPassword
docker compose up -d sqlserver

if ($LASTEXITCODE -ne 0) {
    throw "Failed to start SQL Server with Docker Compose."
}

Write-Host "Waiting for SQL Server to accept connections..."
$deadline = (Get-Date).AddSeconds(90)
$sqlcmd = "/opt/mssql-tools18/bin/sqlcmd"
$sqlcmdFallback = "/opt/mssql-tools/bin/sqlcmd"

do {
    docker exec $ContainerName bash -c "$sqlcmd -C -S localhost -U sa -P '$SqlPassword' -Q 'SELECT 1' || $sqlcmdFallback -C -S localhost -U sa -P '$SqlPassword' -Q 'SELECT 1'" | Out-Null
    if ($LASTEXITCODE -eq 0) {
        break
    }

    Start-Sleep -Seconds 3
} while ((Get-Date) -lt $deadline)

if ($LASTEXITCODE -ne 0) {
    throw "SQL Server container did not become ready in time."
}

$databaseSql = @"
IF DB_ID(N'CivicSync_HomeAffairs') IS NULL CREATE DATABASE [CivicSync_HomeAffairs];
IF DB_ID(N'CivicSync_Sars') IS NULL CREATE DATABASE [CivicSync_Sars];
IF DB_ID(N'CivicSync_Municipality') IS NULL CREATE DATABASE [CivicSync_Municipality];
"@

docker exec $ContainerName bash -c "$sqlcmd -C -S localhost -U sa -P '$SqlPassword' -Q ""$databaseSql"" || $sqlcmdFallback -C -S localhost -U sa -P '$SqlPassword' -Q ""$databaseSql"""

if ($LASTEXITCODE -ne 0) {
    throw "Failed to create CivicSync SQL Server databases."
}

Write-Host "SQL Server is ready."
Write-Host "Set this for migrations if you changed the password:"
Write-Host "`$env:CIVICSYNC_SQL_PASSWORD = `"$SqlPassword`""
