param(
    [string]$SqlServer = "localhost,1433",

    [string]$SqlUser = "sa",

    [string]$SqlPassword = $env:CIVICSYNC_SQL_PASSWORD
)

$ErrorActionPreference = "Stop"

$nodes = @("HomeAffairs", "Sars", "Municipality")

foreach ($node in $nodes) {
    $args = @(
        "-ExecutionPolicy", "Bypass",
        "-File", ".\scripts\migrate-node.ps1",
        "-Node", $node,
        "-SqlServer", $SqlServer,
        "-SqlUser", $SqlUser
    )

    if (-not [string]::IsNullOrWhiteSpace($SqlPassword)) {
        $args += @("-SqlPassword", $SqlPassword)
    }

    & powershell @args
}
