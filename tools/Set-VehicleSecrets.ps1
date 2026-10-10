param([switch]$Database,[switch]$DvlaKey,[switch]$Proxy)
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
if(-not ($Database -or $DvlaKey -or $Proxy)) {throw 'Choose -Database, -DvlaKey or -Proxy.'}
New-Item -ItemType Directory -Force -Path (Split-Path $path -Parent)|Out-Null
[IO.File]::WriteAllText($path,($config|ConvertTo-Json -Depth 10))
Write-Output 'VehicleService User Secrets saved. Values were not printed.'
