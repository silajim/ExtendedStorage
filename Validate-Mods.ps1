[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GamePath,
    [ValidateSet('Core','All')][string]$Mode = 'Core',
    [string]$HarmonyPath,
    [Parameter(Mandatory=$true)][string]$ExtensionPath,
    [switch]$DeepStorage,
    [string]$DeepStoragePath,
    [string]$StockpileLimitPath,
    [ValidatePattern('^[A-Za-z0-9_-]+$')][string]$TestGameName = 'Game',
    [switch]$NoRestore
)
$ErrorActionPreference = 'Stop'
$GamePath = (Resolve-Path -LiteralPath $GamePath).Path
if (!$HarmonyPath) { $HarmonyPath = Join-Path $GamePath 'Mods\HarmonyRimWorld' }
$HarmonyPath = (Resolve-Path -LiteralPath $HarmonyPath).Path
$testRoot = Join-Path $PSScriptRoot '.validation'
$testGame = Join-Path $testRoot $TestGameName
$testMod = Join-Path $testRoot 'RuntimeChecks'
$profile = Join-Path $testRoot ('Profile-' + $Mode + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
if ($DeepStorage) { $profile += '-DeepStorage' }
New-Item -ItemType Directory -Force -Path $testGame,(Join-Path $testGame 'Mods'),(Join-Path $testMod 'About'),(Join-Path $profile 'Config') | Out-Null
function Add-TestJunction([string]$Path, [string]$Target) {
    if (!(Test-Path -LiteralPath $Path)) { New-Item -ItemType Junction -Path $Path -Target $Target | Out-Null }
    elseif ((Get-Item -LiteralPath $Path).Target -ne $Target) { throw "Existing test junction has a different target: $Path" }
}
foreach ($part in @('Data','MonoBleedingEdge','RimWorldWin64_Data')) { Add-TestJunction (Join-Path $testGame $part) (Join-Path $GamePath $part) }
foreach ($part in @('RimWorldWin64.exe','UnityPlayer.dll','UnityCrashHandler64.exe','Version.txt','steam_appid.txt')) { Copy-Item -LiteralPath (Join-Path $GamePath $part) -Destination $testGame -Force }
Add-TestJunction (Join-Path $testGame 'Mods\Harmony') $HarmonyPath
Add-TestJunction (Join-Path $testGame 'Mods\RuntimeChecks') $testMod
Add-TestJunction (Join-Path $testGame 'Mods\ExtendedStorage') $PSScriptRoot
Add-TestJunction (Join-Path $testGame 'Mods\ExtendedStorageExtended') (Resolve-Path -LiteralPath $ExtensionPath).Path
if ($DeepStorage) {
    if (!$DeepStoragePath) { $DeepStoragePath = Join-Path $GamePath 'Mods\3532608331' }
    if (!$StockpileLimitPath) { $StockpileLimitPath = Join-Path $GamePath 'Mods\Stockpile Limit forked for 1.4' }
    Add-TestJunction (Join-Path $testGame 'Mods\DeepStorage') (Resolve-Path -LiteralPath $DeepStoragePath).Path
    Add-TestJunction (Join-Path $testGame 'Mods\StockpileLimit') (Resolve-Path -LiteralPath $StockpileLimitPath).Path
}
$about = '<ModMetaData><name>Storage 1.6 runtime checks</name><author>Local</author><packageId>local.storage16checks</packageId><supportedVersions><li>1.6</li></supportedVersions><loadAfter><li>mlie.extendedstorageextended</li></loadAfter></ModMetaData>'
Set-Content -LiteralPath (Join-Path $testMod 'About\About.xml') -Value $about
$buildArgs = @('build', (Join-Path $PSScriptRoot 'Tests\RuntimeChecks\RuntimeChecks.csproj'), '-c', 'Release', ('-p:GamePath=' + $GamePath), ('-p:HarmonyAssemblyPath=' + (Join-Path $HarmonyPath '1.6\Assemblies\0Harmony.dll')))
if ($NoRestore) { $buildArgs += '--no-restore' }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw 'Runtime check build failed' }
$expansions = @('ludeon.rimworld.royalty','ludeon.rimworld.ideology','ludeon.rimworld.biotech','ludeon.rimworld.anomaly','ludeon.rimworld.odyssey')
$active = @('brrainz.harmony','ludeon.rimworld')
if ($Mode -eq 'All') { $active += $expansions }
$active += @('mlie.extendedstorage','mlie.extendedstorageextended')
if ($DeepStorage) { $active += @('lwm.deepstorage','darkside.stockpilelimit.continuedcactuspie') }
$active += 'local.storage16checks'
$config = '<ModsConfigData><version>1.6.4871 rev591</version><activeMods>' + (($active | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</activeMods><knownExpansions>' + (($expansions | ForEach-Object { '<li>' + $_ + '</li>' }) -join '') + '</knownExpansions></ModsConfigData>'
Set-Content -LiteralPath (Join-Path $profile 'Config\ModsConfig.xml') -Value $config
$logPath = Join-Path $profile 'Player.log'
$arguments = @('-batchmode','-quicktest','-logFile', ('"' + $logPath + '"'), ('"-savedatafolder=' + $profile + '"'))
$process = Start-Process -FilePath (Join-Path $testGame 'RimWorldWin64.exe') -WorkingDirectory $testGame -ArgumentList $arguments -WindowStyle Hidden -PassThru
$process.Id | Set-Content -LiteralPath (Join-Path $profile 'process-id.txt')
Write-Output "Checks started. PID: $($process.Id)"
Write-Output "Result: $(Join-Path $profile 'result.txt')"
Write-Output "Log: $logPath"
