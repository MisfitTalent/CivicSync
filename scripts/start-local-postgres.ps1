param(
    [string]$ContainerName = "civicsync-postgres",

    [string]$PostgresPassword = $env:CIVICSYNC_POSTGRES_PASSWORD
)

$ErrorActionPreference = "Stop"

docker info | Out-Null

if ($LASTEXITCODE -ne 0) {
    throw "Docker is not running. Start Docker Desktop with the Linux engine enabled, then run this script again."
}

if ([string]::IsNullOrWhiteSpace($PostgresPassword)) {
    $PostgresPassword = "postgres"
    Write-Warning "CIVICSYNC_POSTGRES_PASSWORD is not set. Using development password 'postgres'."
}

$env:CIVICSYNC_POSTGRES_PASSWORD = $PostgresPassword
docker compose up -d postgres

if ($LASTEXITCODE -ne 0) {
    throw "Failed to start PostgreSQL with Docker Compose."
}

Write-Host "Waiting for PostgreSQL to accept connections..."
$deadline = (Get-Date).AddSeconds(60)

do {
    docker exec $ContainerName pg_isready -U postgres | Out-Null
    if ($LASTEXITCODE -eq 0) {
        break
    }

    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)

if ($LASTEXITCODE -ne 0) {
    throw "PostgreSQL container did not become ready in time."
}

Write-Host "PostgreSQL is ready."
