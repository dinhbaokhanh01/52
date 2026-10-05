param([string]$Database = 'CareerPortalExtended', [int]$Port = 5134)
$ErrorActionPreference = 'Stop'
if ($Database -notmatch '^[A-Za-z][A-Za-z0-9_]{0,100}$') { throw 'Invalid database name.' }
if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Port must be between 1024 and 65535.' }

# ASCII-only source: compatible with Windows PowerShell 5.1 and PowerShell 7.
# Discover the project without embedding a Vietnamese directory name.
$taskBackend = Join-Path $PSScriptRoot 'backend'
$taskProjects = @(Get-ChildItem -LiteralPath $taskBackend -Filter 'CareerPortal.Api.csproj' -File -Recurse)
if ($taskProjects.Count -ne 1) { throw 'Expected one CareerPortal.Api.csproj inside backend. Place this script next to the backend folder.' }
$taskProject = $taskProjects[0].FullName

$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ConnectionStrings__CareerPortal = "Server=(localdb)\MSSQLLocalDB;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True;"
$env:PublicUrl = "http://localhost:$Port"
if (-not $env:Smtp__Host) {
    $env:Smtp__DeliveryMode = 'PickupDirectory'
    $env:Smtp__From = 'portal@example.test'
    Write-Host 'Demo email: saved as .eml in the API App_Data\mail folder; not sent online.'
}
else { $env:Smtp__DeliveryMode = 'Network' }

if (-not $env:BootstrapToken) {
    $taskTokenBytes = New-Object byte[] 24
    $taskRandom = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $taskRandom.GetBytes($taskTokenBytes) }
    finally { $taskRandom.Dispose() }
    $env:BootstrapToken = [System.BitConverter]::ToString($taskTokenBytes).Replace('-', '')
}
Write-Host "Open http://localhost:$Port/index.html"
Write-Host "First Admin setup token: $env:BootstrapToken"
Write-Host 'Keep this token private. It only works before the first Admin exists.'
dotnet restore $taskProject
if ($LASTEXITCODE -ne 0) { throw 'NuGet restore failed. Check your Internet connection and NuGet error above.' }
dotnet ef database update --project $taskProject
if ($LASTEXITCODE -ne 0) { throw 'Migration failed. Check .NET SDK 10, dotnet-ef and SQL Server LocalDB.' }
dotnet run --project $taskProject --no-launch-profile --urls "http://localhost:$Port"
if ($LASTEXITCODE -ne 0) { throw 'Application exited with an error. See the output above.' }
