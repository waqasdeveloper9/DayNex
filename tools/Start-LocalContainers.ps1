param([switch]$Build,[switch]$Vehicle)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
function Read-Connection([string]$secretId) {
 $path = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretId\secrets.json"
 if (-not (Test-Path -LiteralPath $path)) { throw "User Secrets not found for $secretId" }
 $config = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
 $value = $config.'ConnectionStrings:DefaultConnection'
 if (-not $value) { $value = $config.ConnectionStrings.DefaultConnection }
 if ([string]::IsNullOrWhiteSpace($value)) { throw "Database connection missing for $secretId" }
 return $value
}
$keys = @('DAYNEX_IDENTITY_CONNECTION','DAYNEX_HOLIDAY_CONNECTION','DAYNEX_VEHICLE_CONNECTION','DAYNEX_DVLA_ENABLED','DAYNEX_DVLA_KEY','DAYNEX_PROXY_ENABLED','DAYNEX_PROXY_ADDRESS','DAYNEX_PROXY_USERNAME','DAYNEX_PROXY_PASSWORD')
$original = @{}
foreach ($key in $keys) { $original[$key] = [Environment]::GetEnvironmentVariable($key,'Process') }
Push-Location $taskRoot
try {
 $env:DAYNEX_IDENTITY_CONNECTION = Read-Connection 'DayNex-IdentityService-Development'
 $env:DAYNEX_HOLIDAY_CONNECTION = Read-Connection 'DayNex-HolidayService-Development'
 $arguments = @('compose','up','--detach')
 if ($Build) { $arguments += '--build' }
 & docker @arguments
 if ($LASTEXITCODE -ne 0) { throw 'Docker Compose failed.' }
} finally {
 foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key,$original[$key],'Process') }
 Pop-Location
}