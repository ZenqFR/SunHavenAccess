<#
.SYNOPSIS
    Assemble la charge utile embarquée dans l'installateur, puis compile celui-ci.

.DESCRIPTION
    L'installateur embarque tout ce qu'il doit poser : BepInEx et le mod. Ainsi il n'y a qu'un
    seul fichier à distribuer, l'installation fonctionne hors ligne, et rien ne casse le jour où
    une URL de téléchargement change.

    Ce script prépare installer/payload/ en reproduisant EXACTEMENT l'arborescence attendue dans
    le dossier du jeu, puis lance la compilation. Chaque fichier de payload/ sera copié tel quel,
    au même chemin relatif, dans le dossier de Sun Haven.

    BepInEx doit être fourni séparément : il est sous licence LGPL-2.1, redistribuable, mais sa
    licence doit accompagner la distribution — d'où la vérification explicite ci-dessous plutôt
    qu'une copie silencieuse.

.PARAMETER BepInExZip
    Chemin vers l'archive officielle BepInEx 5.4.23.5 x64 (Mono) téléchargée depuis
    https://github.com/BepInEx/BepInEx/releases

.EXAMPLE
    .\build-payload.ps1 -BepInExZip "$HOME\Downloads\BepInEx_x64_5.4.23.5.zip"
#>
param(
    [Parameter(Mandatory = $false)]
    [string]$BepInExZip
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent $here
$payload = Join-Path $here "payload"
$payloadZip = Join-Path $here "payload.zip"

Write-Host "== Assemblage de la charge utile ==" -ForegroundColor Cyan

# --- 1. Repartir d'une charge utile propre -----------------------------------------------
if (Test-Path $payload) { Remove-Item $payload -Recurse -Force }
New-Item -ItemType Directory -Path $payload | Out-Null

# --- 2. Le mod ---------------------------------------------------------------------------
# Compilé d'abord, pour embarquer la version courante et non un reliquat.
Write-Host "Compilation du mod..."
Push-Location $repo
try { dotnet build -c Release | Out-Null }
finally { Pop-Location }

$pluginDir = Join-Path $payload 'BepInEx\plugins\SunHavenAccess'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null

$modDll = Join-Path $repo 'bin\Release\SunHavenAccess.dll'
if (-not (Test-Path $modDll)) { throw "SunHavenAccess.dll est introuvable apres compilation : $modDll" }
Copy-Item $modDll $pluginDir

# Les DLL natives de synthese vocale, redistribuees avec le mod.
foreach ($native in @('Tolk.dll', 'nvdaControllerClient64.dll')) {
    $source = Join-Path $repo "lib\$native"
    if (Test-Path $source) {
        Copy-Item $source $pluginDir
    } else {
        Write-Warning "Fichier natif absent, la synthese vocale pourrait ne pas fonctionner : $native"
    }
}
Write-Host "  Mod ajoute." -ForegroundColor Green

# --- 3. BepInEx --------------------------------------------------------------------------
if ($BepInExZip) {
    if (-not (Test-Path $BepInExZip)) { throw "Archive BepInEx introuvable : $BepInExZip" }

    Write-Host "Extraction de BepInEx..."
    $temp = Join-Path $env:TEMP "bepinex-payload-$(Get-Random)"
    New-Item -ItemType Directory -Path $temp | Out-Null
    try {
        Expand-Archive -Path $BepInExZip -DestinationPath $temp -Force
        Copy-Item (Join-Path $temp '*') $payload -Recurse -Force
        Write-Host "  BepInEx ajoute." -ForegroundColor Green
    }
    finally {
        Remove-Item $temp -Recurse -Force -ErrorAction SilentlyContinue
    }

    # La licence de BepInEx DOIT accompagner sa redistribution (LGPL-2.1).
    $license = Join-Path $here 'LICENSE-BepInEx.txt'
    if (-not (Test-Path $license)) {
        Write-Warning "LICENSE-BepInEx.txt est absent de installer/."
        Write-Warning "BepInEx est sous LGPL-2.1 : sa licence doit accompagner toute redistribution."
        Write-Warning "Recuperez-la sur https://github.com/BepInEx/BepInEx/blob/master/LICENSE"
    } else {
        Copy-Item $license (Join-Path $payload 'BepInEx-LICENSE.txt')
    }
}
else {
    Write-Warning "Aucune archive BepInEx fournie : la charge utile ne contiendra QUE le mod."
    Write-Warning "L'installateur supposera alors que BepInEx est deja installe chez l'utilisateur."
    Write-Warning "Pour une distribution complete, relancez avec -BepInExZip <chemin de l'archive>."
}

# --- 3b. Archive unique ------------------------------------------------------------------
# L.installateur embarque UNE archive plutot que les fichiers un par un : MSBuild transforme les
# separateurs de dossier en points dans les noms de ressources, ce qui devient ambigu avec les
# extensions. Une archive preserve l.arborescence exacte, sans reconstruction de chemins.
if (Test-Path $payloadZip) { Remove-Item $payloadZip -Force }
Compress-Archive -Path (Join-Path $payload "*") -DestinationPath $payloadZip -Force
Write-Host "  Archive creee." -ForegroundColor Green

# --- 4. Compilation de l'installateur -----------------------------------------------------
Write-Host "Compilation de l'installateur..."
Push-Location $here
try { dotnet build -c Release | Out-Null }
finally { Pop-Location }

$exe = Join-Path $here 'bin\Release\Installer Sun Haven Access.exe'
if (Test-Path $exe) {
    $size = [math]::Round((Get-Item $exe).Length / 1MB, 2)
    Write-Host ""
    Write-Host "== Termine ==" -ForegroundColor Cyan
    Write-Host "Installateur : $exe ($size Mo)" -ForegroundColor Green
    Write-Host ""
    Write-Host "A verifier avant publication :" -ForegroundColor Yellow
    Write-Host "  - Tester sur un dossier de jeu ou le mod n'est PAS deja installe."
    Write-Host "  - Verifier la lecture NVDA de chaque controle."
    Write-Host "  - Un .exe non signe declenche SmartScreen : la procedure doit etre documentee."
} else {
    throw "L'installateur n'a pas ete produit."
}
