<#
.SYNOPSIS
    Vérifie l'installateur sur de FAUX dossiers de jeu, sans toucher à l'installation réelle.

.DESCRIPTION
    Le test qui compte est le premier : une installation depuis un dossier totalement vierge.
    Installer par-dessus une installation existante ne prouve rien — c'est précisément le cas où
    tout est déjà en place. Or c'est la situation de tout nouvel utilisateur.

    La logique d'installation étant séparée de l'interface, elle s'appelle directement par
    réflexion : pas de fenêtre à piloter, et le compte rendu est capturé tel qu'il s'afficherait.

.EXAMPLE
    .\test-installer.ps1
#>
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sp = Join-Path $env:TEMP 'sunhaven-access-tests'
if (-not (Test-Path $sp)) { New-Item -ItemType Directory -Path $sp | Out-Null }
$exe = Join-Path $here 'bin\Release\Installer Sun Haven Access.exe'
if (-not (Test-Path $exe)) { throw "Installateur introuvable. Lancez d'abord build-payload.ps1." }
$asm = [Reflection.Assembly]::LoadFrom($exe)
$mi = $asm.GetType('SunHavenAccess.Installer.ModInstaller')
$gl = $asm.GetType('SunHavenAccess.Installer.GameLocator')
$rt = $asm.GetType('SunHavenAccess.Installer.ModInstaller+Report')

$script:pass = 0
$script:fail = 0
function Check($label, $actual, $expected) {
    if ($actual -eq $expected) { $script:pass++; Write-Output "  OK   $label" }
    else { $script:fail++; Write-Output "  ECHEC $label -> attendu '$expected', obtenu '$actual'" }
}
function NewFake($name) {
    $d = Join-Path $sp $name
    if (Test-Path $d) { Remove-Item $d -Recurse -Force }
    New-Item -ItemType Directory -Path $d | Out-Null
    Set-Content -Path (Join-Path $d 'Sun Haven.exe') -Value 'faux' -Encoding utf8
    return [string]$d
}
function Run($method, $dir) {
    $global:log = New-Object System.Collections.Generic.List[string]
    $del = [scriptblock]::Create('param($m) $global:log.Add([string]$m)') -as $rt
    return $mi.GetMethod($method).Invoke($null, [object[]]@([string]$dir, $del))
}

Write-Output "=== 1. Installation a froid (dossier totalement vierge) ==="
$d = NewFake 'T1'
Check "BepInEx absent au depart" (icm { $mi.GetMethod('BepInExPresent').Invoke($null,[object[]]@($d)) }) $false
Check "Mod absent au depart"     (icm { $mi.GetMethod('IsInstalled').Invoke($null,[object[]]@($d)) }) $false
Check "Install reussit"          (Run 'Install' $d) $true
Check "BepInEx pose"             (icm { $mi.GetMethod('BepInExPresent').Invoke($null,[object[]]@($d)) }) $true
Check "Mod pose"                 (icm { $mi.GetMethod('IsInstalled').Invoke($null,[object[]]@($d)) }) $true
Check "Licence LGPL livree"      (Test-Path (Join-Path $d 'BepInEx-LICENSE.txt')) $true

Write-Output ""
Write-Output "=== 2. Reinstallation par-dessus (mise a jour) ==="
Check "Reinstall reussit"        (Run 'Install' $d) $true
Check "Mod toujours la"          (icm { $mi.GetMethod('IsInstalled').Invoke($null,[object[]]@($d)) }) $true

Write-Output ""
Write-Output "=== 3. Desinstallation ==="
Check "Uninstall reussit"        (Run 'Uninstall' $d) $true
Check "Mod retire"               (icm { $mi.GetMethod('IsInstalled').Invoke($null,[object[]]@($d)) }) $false
Check "BepInEx CONSERVE"         (icm { $mi.GetMethod('BepInExPresent').Invoke($null,[object[]]@($d)) }) $true
Check "Uninstall a vide tolere"  (Run 'Uninstall' $d) $true

Write-Output ""
Write-Output "=== 4. Dossier invalide (pas Sun Haven.exe) ==="
$bad = Join-Path $sp 'T4'
if (Test-Path $bad) { Remove-Item $bad -Recurse -Force }
New-Item -ItemType Directory -Path $bad | Out-Null
Check "Install refusee"          (Run 'Install' ([string]$bad)) $false
Check "Rien pose"                ((Get-ChildItem $bad -Recurse -File).Count) 0
Check "Uninstall refusee"        (Run 'Uninstall' ([string]$bad)) $false

Write-Output ""
Write-Output "=== 5. Detection automatique du jeu ==="
$found = $gl.GetMethod('FindGameDirectory').Invoke($null, @())
Write-Output "  Detecte : $found"
Check "Jeu reel trouve"          (icm { $gl.GetMethod('IsGameDirectory').Invoke($null,[object[]]@([string]$found)) }) $true

Write-Output ""
Write-Output "======================================"
Write-Output "  $script:pass reussis, $script:fail echecs"
Write-Output "======================================"
foreach ($n in @('T1','T4')) {
    $p = Join-Path $sp $n
    if (Test-Path $p) { Remove-Item $p -Recurse -Force }
}
