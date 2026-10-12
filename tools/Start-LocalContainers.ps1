param([switch]$Build, [switch]$Vehicle)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent

function Read-UserSecrets([string]$secretId) {
    $path = Join-Path $env:APPDATA "Microsoft\UserSecrets\$secretId\secrets.json"
    if (-not (Test-Path -LiteralPath $path)) { throw "User Secrets not found for $secretId" }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-SecretValue($config, [string]$name) {
    $flat = $config.PSObject.Properties[$name]
    if ($null -ne $flat) { return $flat.Value }
    $value = $config
    foreach ($part in $name.Split(':')) {
        if ($null -eq $value) { return $null }
        $value = $value.$part
    }
    return $value
}

function Read-Connection([string]$secretId) {
    $value = Get-SecretValue (Read-UserSecrets $secretId) 'ConnectionStrings:DefaultConnection'
    if ([string]::IsNullOrWhiteSpace($value)) { throw "Database connection missing for $secretId" }
    return $value
}

$vehicleVariables = @{
    DAYNEX_REFRESH_ENABLED = 'VehicleRefresh:Enabled'
    DAYNEX_REFRESH_KEY = 'VehicleRefresh:ApiKey'
    DAYNEX_DVLA_ENABLED = 'Dvla:Enabled'
    DAYNEX_DVLA_KEY = 'Dvla:ApiKey'
    DAYNEX_PROXY_ENABLED = 'Dvla:Proxy:Enabled'
    DAYNEX_PROXY_ADDRESS = 'Dvla:Proxy:Address'
    DAYNEX_PROXY_USERNAME = 'Dvla:Proxy:Username'
    DAYNEX_PROXY_PASSWORD = 'Dvla:Proxy:Password'
    DAYNEX_MOT_ENABLED = 'Mot:Enabled'
    DAYNEX_MOT_TOKEN_ENDPOINT = 'Mot:TokenEndpoint'
    DAYNEX_MOT_CLIENT_ID = 'Mot:ClientId'
    DAYNEX_MOT_CLIENT_SECRET = 'Mot:ClientSecret'
    DAYNEX_MOT_API_KEY = 'Mot:ApiKey'
}
$keys = @('DAYNEX_IDENTITY_CONNECTION', 'DAYNEX_HOLIDAY_CONNECTION', 'DAYNEX_VEHICLE_CONNECTION') +
    @($vehicleVariables.Keys)
$original = @{}
foreach ($key in $keys) { $original[$key] = [Environment]::GetEnvironmentVariable($key, 'Process') }

Push-Location $taskRoot
try {
    $env:DAYNEX_IDENTITY_CONNECTION = Read-Connection 'DayNex-IdentityService-Development'
    $env:DAYNEX_HOLIDAY_CONNECTION = Read-Connection 'DayNex-HolidayService-Development'
    $arguments = @('compose', '-f', 'compose.yaml')
    if ($Vehicle) {
        $env:DAYNEX_VEHICLE_CONNECTION = Read-Connection 'DayNex-VehicleService-Development'
        $config = Read-UserSecrets 'DayNex-VehicleService-Development'
        foreach ($key in $vehicleVariables.Keys) {
            $value = Get-SecretValue $config $vehicleVariables[$key]
            [Environment]::SetEnvironmentVariable($key, [string]$value, 'Process')
        }
        $arguments += @('-f', 'compose.vehicle.yaml')
    }
    $arguments += @('up', '--detach')
    if ($Build) { $arguments += '--build' }
    & docker @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Docker Compose failed.' }
}
finally {
    foreach ($key in $keys) { [Environment]::SetEnvironmentVariable($key, $original[$key], 'Process') }
    Pop-Location
}
