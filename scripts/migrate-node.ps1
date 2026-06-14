param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("HomeAffairs", "Sars", "Municipality")]
    [string]$Node,

    [string]$SqlServer = "localhost,1433",

    [string]$SqlUser = "sa",

    [string]$SqlPassword = $env:CIVICSYNC_SQL_PASSWORD
)

$ErrorActionPreference = "Stop"

$nodeConfigs = @{
    HomeAffairs = @{
        Database = "CivicSync_HomeAffairs"
        DepartmentCode = "1"
        ApiBaseUrl = "http://localhost:5076"
        Peers = @(
            @{ DepartmentCode = "2"; ApiBaseUrl = "http://localhost:5077" },
            @{ DepartmentCode = "3"; ApiBaseUrl = "http://localhost:5078" }
        )
    }
    Sars = @{
        Database = "CivicSync_Sars"
        DepartmentCode = "2"
        ApiBaseUrl = "http://localhost:5077"
        Peers = @(
            @{ DepartmentCode = "1"; ApiBaseUrl = "http://localhost:5076" },
            @{ DepartmentCode = "3"; ApiBaseUrl = "http://localhost:5078" }
        )
    }
    Municipality = @{
        Database = "CivicSync_Municipality"
        DepartmentCode = "3"
        ApiBaseUrl = "http://localhost:5078"
        Peers = @(
            @{ DepartmentCode = "1"; ApiBaseUrl = "http://localhost:5076" },
            @{ DepartmentCode = "2"; ApiBaseUrl = "http://localhost:5077" }
        )
    }
}

$config = $nodeConfigs[$Node]

if ([string]::IsNullOrWhiteSpace($SqlPassword)) {
    throw "SQL Server password missing. Set CIVICSYNC_SQL_PASSWORD or pass -SqlPassword."
}

$connectionString = "Server=$SqlServer;Database=$($config.Database);User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True"

$env:ConnectionStrings__CivicSyncNode = $connectionString
$env:Node__DepartmentCode = $config.DepartmentCode
$env:Node__ApiBaseUrl = $config.ApiBaseUrl
$env:Node__SharedSecret = if ([string]::IsNullOrWhiteSpace($env:CIVICSYNC_NODE_SHARED_SECRET)) { "development-node-sync-secret" } else { $env:CIVICSYNC_NODE_SHARED_SECRET }
$env:Node__MaxSyncPublishAttempts = "3"

for ($index = 0; $index -lt $config.Peers.Count; $index++) {
    $peer = $config.Peers[$index]
    Set-Item -Path "Env:Node__Peers__$index__DepartmentCode" -Value $peer.DepartmentCode
    Set-Item -Path "Env:Node__Peers__$index__ApiBaseUrl" -Value $peer.ApiBaseUrl
}

Write-Host "Migrating $Node node database '$($config.Database)' on SQL Server '$SqlServer'..."
Write-Host "Node API URL: $($config.ApiBaseUrl)"
Write-Host "Peer count: $($config.Peers.Count)"

dotnet run --project .\aspnet-core\src\CivicSync.Migrator\CivicSync.Migrator.csproj
