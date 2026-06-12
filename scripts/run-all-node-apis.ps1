$ErrorActionPreference = "Stop"

$nodes = @("HomeAffairs", "Sars", "Municipality")

foreach ($node in $nodes) {
    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-ExecutionPolicy", "Bypass",
        "-File", "$(Resolve-Path .\scripts\run-node-api.ps1)",
        "-Node", $node
    )
}
