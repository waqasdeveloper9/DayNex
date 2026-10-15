param([switch]$Database,[switch]$DvlaKey,[switch]$Proxy,[switch]$Mot,[switch]$RefreshJob,[switch]$CompaniesHouse)
$ErrorActionPreference='Stop'
$path=Join-Path $env:APPDATA 'Microsoft\UserSecrets\DayNex-VehicleService-Development\secrets.json'
$config=@{}
if(Test-Path -LiteralPath $path) {$config=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json -AsHashtable}
function Read-Secret([string]$prompt) {
 $secure=Read-Host $prompt -AsSecureString
 return [System.Net.NetworkCredential]::new('', $secure).Password
}
if($Database) {$config['ConnectionStrings:DefaultConnection']=Read-Secret 'Vehicle database connection string (hidden)'}
if($DvlaKey) {
 $config['Dvla:ApiKey']=Read-Secret 'Fresh DVLA VES API key (hidden)'
 $config['Dvla:Enabled']=$true
}
if($Proxy) {
 $config['Dvla:Proxy:Address']=Read-Host 'Approved proxy URL'
 $config['Dvla:Proxy:Username']=Read-Host 'Proxy username'
 $config['Dvla:Proxy:Password']=Read-Secret 'Proxy password (hidden)'
 $config['Dvla:Proxy:Enabled']=$true
}
if($Mot) {
 $config['Mot:TokenEndpoint']=Read-Host 'Access token URL from your NEW DVSA credentials email'
 $config['Mot:ClientId']=Read-Host 'DVSA application client ID (not the DayNex client ID)'
 $config['Mot:ClientSecret']=Read-Secret 'New DVSA client secret VALUE (hidden)'
 $config['Mot:ApiKey']=Read-Secret 'New DVSA API key (hidden)'
 $config['Mot:Scope']='https://tapi.dvsa.gov.uk/.default'
 $config['Mot:Enabled']=$true
}
if($CompaniesHouse) {
 $config['CompaniesHouse:ApiKey']=Read-Secret 'Companies House REST API key (hidden)'
 $config['CompaniesHouse:Enabled']=$true
}
if($RefreshJob) {
 $config['VehicleRefresh:ApiKey']=Read-Secret 'Separate random refresh job key (32 or more characters, hidden)'
 if($config['VehicleRefresh:ApiKey'].Length -lt 32) {throw 'Job key must have at least 32 characters.'}
 $config['VehicleRefresh:Enabled']=$true
}
if(-not ($Database -or $DvlaKey -or $Proxy -or $Mot -or $RefreshJob -or $CompaniesHouse)) {throw 'Choose -Database, -DvlaKey, -Proxy, -Mot, -RefreshJob or -CompaniesHouse.'}
New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent)|Out-Null
[IO.File]::WriteAllText($path,($config|ConvertTo-Json -Depth 10))
Write-Output 'UserInfoService User Secrets saved. Values were not printed.'
