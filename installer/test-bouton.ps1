<#
.SYNOPSIS
    Vérifie que le bouton principal dit « Installer » ou « Mettre à jour » selon l'état réel du
    dossier saisi.

.DESCRIPTION
    Pilote la vraie fenêtre, sans l'afficher : c'est le seul moyen de prouver que le libellé suit
    bien la saisie et les opérations, plutôt que de re-tester la logique d'installation, déjà
    couverte par test-installer.ps1.

    Ce que lit un lecteur d'écran, c'est AccessibleName quand il est défini, pas Text — les deux
    sont donc vérifiés ensemble.
#>
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
Add-Type -AssemblyName System.Windows.Forms

$exe = Join-Path $here 'bin\Release\Installer Sun Haven Access.exe'
if (-not (Test-Path $exe)) { throw "Installateur introuvable. Lancez d'abord build-payload.ps1." }
$asm = [Reflection.Assembly]::LoadFrom($exe)
$formType = $asm.GetType('SunHavenAccess.Installer.MainForm')
$mi = $asm.GetType('SunHavenAccess.Installer.ModInstaller')
$rt = $asm.GetType('SunHavenAccess.Installer.ModInstaller+Report')

$sp = Join-Path $env:TEMP 'sunhaven-access-tests'
if (-not (Test-Path $sp)) { New-Item -ItemType Directory -Path $sp | Out-Null }
$fake = Join-Path $sp 'BoutonTest'
if (Test-Path $fake) { Remove-Item $fake -Recurse -Force }
New-Item -ItemType Directory -Path $fake | Out-Null
Set-Content -Path (Join-Path $fake 'Sun Haven.exe') -Value 'faux' -Encoding utf8

$script:pass = 0
$script:fail = 0
function Check($label, $actual, $expected) {
    if ($actual -eq $expected) { $script:pass++; Write-Output "  OK   $label" }
    else { $script:fail++; Write-Output "  ECHEC $label -> attendu '$expected', obtenu '$actual'" }
}

# Accès aux contrôles privés de la fenêtre : ils n'ont pas à être publics pour le seul test.
$flags = [Reflection.BindingFlags]::NonPublic -bor [Reflection.BindingFlags]::Instance
$form = [Activator]::CreateInstance($formType)
$pathBox = $formType.GetField('_pathBox', $flags).GetValue($form)
$button  = $formType.GetField('_installButton', $flags).GetValue($form)

function State() { return "$($button.Text) / $($button.AccessibleName)" }

Write-Output "=== Dossier vierge ==="
$pathBox.Text = $fake
Check "Libelle" (State) "&Installer / Installer le mod"

Write-Output ""
Write-Output "=== Apres installation ==="
$del = [scriptblock]::Create('param($m)') -as $rt
[void]$mi.GetMethod('Install').Invoke($null, [object[]]@([string]$fake, $del))
# On force le rafraichissement comme le fait la fenetre apres une operation.
$formType.GetMethod('RefreshActionLabel', $flags).Invoke($form, @())
Check "Libelle" (State) "Mettre à &jour / Mettre à jour le mod"

Write-Output ""
Write-Output "=== Chemin change vers un dossier vierge ==="
$autre = Join-Path $sp 'BoutonTest2'
if (Test-Path $autre) { Remove-Item $autre -Recurse -Force }
New-Item -ItemType Directory -Path $autre | Out-Null
Set-Content -Path (Join-Path $autre 'Sun Haven.exe') -Value 'faux' -Encoding utf8
$pathBox.Text = $autre
Check "Libelle" (State) "&Installer / Installer le mod"

Write-Output ""
Write-Output "=== Retour sur le dossier installe ==="
$pathBox.Text = $fake
Check "Libelle suit la saisie" ($button.Text -like 'Mettre*') $true

Write-Output ""
Write-Output "=== Apres desinstallation ==="
[void]$mi.GetMethod('Uninstall').Invoke($null, [object[]]@([string]$fake, $del))
$formType.GetMethod('RefreshActionLabel', $flags).Invoke($form, @())
Check "Libelle" (State) "&Installer / Installer le mod"

Write-Output ""
Write-Output "=== Raccourci clavier ==="
Check "Un seul & dans le libelle" ((($button.Text.ToCharArray() | Where-Object { $_ -eq '&' }).Count)) 1

$form.Dispose()
Remove-Item $fake -Recurse -Force
Remove-Item $autre -Recurse -Force

Write-Output ""
Write-Output "======================================"
Write-Output "  $script:pass reussis, $script:fail echecs"
Write-Output "======================================"
if ($script:fail -gt 0) { exit 1 }
