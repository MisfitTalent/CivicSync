param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("HomeAffairs", "Sars", "Municipality")]
    [string]$Node
)

$ErrorActionPreference = "Stop"

$profiles = @{
    HomeAffairs = "HomeAffairs"
    Sars = "Sars"
    Municipality = "Municipality"
}

$profile = $profiles[$Node]

Write-Host "Starting CivicSync $Node API with launch profile '$profile'..."
dotnet run --project .\aspnet-core\src\CivicSync.Web.Host\CivicSync.Web.Host.csproj --launch-profile $profile
