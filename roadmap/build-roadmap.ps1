<#
.SYNOPSIS
    Produit roadmap.html (le site) et ROADMAP.md (la copie locale) depuis roadmap/roadmap.json.

.DESCRIPTION
    Une seule source de vérité : roadmap/roadmap.json. Faire évoluer un point, c'est changer son
    champ `status` puis relancer ce script — jamais éditer le HTML à la main, où l'erreur devient
    certaine à force de retouches ponctuelles.

    Statuts : todo (à tester), fix (à corriger), optimize (à optimiser), ok (au point).
    Chacun est rendu EN TEXTE dans la page, jamais par la seule couleur : cette page s'adresse
    d'abord à un lecteur d'écran.

    L'ordre des étapes suit la progression d'un joueur dans Sun Haven, pas l'architecture du mod :
    on teste ce qu'on rencontre, quand on le rencontre.

.EXAMPLE
    .\build-roadmap.ps1
#>
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent $here
$data = Get-Content (Join-Path $here 'roadmap.json') -Raw -Encoding UTF8 | ConvertFrom-Json

function Esc([string]$s) {
    if ($null -eq $s) { return '' }
    return $s.Replace('&', '&amp;').Replace('<', '&lt;').Replace('>', '&gt;')
}
function Bi($obj) {
    return '<span data-lang="fr">' + (Esc $obj.fr) + '</span><span data-lang="en" hidden>' + (Esc $obj.en) + '</span>'
}

$order = @('todo', 'fix', 'optimize', 'ok')
$counts = @{}
foreach ($s in $order) { $counts[$s] = 0 }
$total = 0
foreach ($stage in $data.stages) {
    foreach ($item in $stage.items) {
        if (-not $counts.ContainsKey($item.status)) { throw "Statut inconnu '$($item.status)' pour $($item.id)" }
        $counts[$item.status]++
        $total++
    }
}
$done = $counts['ok']
$percent = if ($total -gt 0) { [math]::Round(100 * $done / $total) } else { 0 }

$sb = [System.Text.StringBuilder]::new()
function Add($line) { [void]$sb.AppendLine($line) }

# --- Bilan ------------------------------------------------------------------------------------
Add '  <h2 class="section-title" id="bilan"><span data-lang="fr">Où en est-on</span><span data-lang="en" hidden>Where things stand</span></h2>'
Add '  <div class="card">'
Add ('    <p class="roadmap-headline"><span data-lang="fr"><strong>' + $done + ' point' + $(if ($done -gt 1) { 's' } else { '' }) + ' au point sur ' + $total + '</strong> — ' + $percent + '&nbsp;%.</span><span data-lang="en" hidden><strong>' + $done + ' of ' + $total + ' solid</strong> — ' + $percent + '%.</span></p>')
Add '    <ul class="legend-list">'
foreach ($s in $order) {
    $l = $data.legend.$s
    Add ('      <li class="legend-' + $s + '"><strong><span data-lang="fr">' + (Esc $l.fr) + '</span><span data-lang="en" hidden>' + (Esc $l.en) + '</span></strong> — <span data-lang="fr">' + (Esc $l.hint.fr) + '&nbsp;: ' + $counts[$s] + '</span><span data-lang="en" hidden>' + (Esc $l.hint.en) + ': ' + $counts[$s] + '</span></li>')
}
Add '    </ul>'
Add '    <p class="note"><span data-lang="fr">« Au point » veut dire : essayé en jeu, avec le comportement attendu observé. Rien n''y passe sans un essai réel — c''est le seul moyen de le savoir. Chaque retour fait avancer cette liste et affine l''accessibilité du mod.</span><span data-lang="en" hidden>"Solid" means: tried in game, with the expected behaviour observed. Nothing gets there without a real trial — that is the only way to know. Every report moves this list along and sharpens the mod''s accessibility.</span></p>'
Add '  </div>'
Add ''

# --- Étapes -----------------------------------------------------------------------------------
$stageNumber = 0
foreach ($stage in $data.stages) {
    $stageNumber++
    $stageTotal = $stage.items.Count
    $stageDone = ($stage.items | Where-Object { $_.status -eq 'ok' }).Count

    Add ('  <h2 class="section-title" id="' + $stage.id + '"><span data-lang="fr">' + $stageNumber + '. ' + (Esc $stage.title.fr) + '</span><span data-lang="en" hidden>' + $stageNumber + '. ' + (Esc $stage.title.en) + '</span></h2>')
    Add '  <div class="card">'
    Add ('    <p>' + (Bi $stage.intro) + '</p>')
    Add ('    <p class="stage-progress"><span data-lang="fr">' + $stageDone + ' sur ' + $stageTotal + ' au point.</span><span data-lang="en" hidden>' + $stageDone + ' of ' + $stageTotal + ' solid.</span></p>')
    Add '    <ol class="roadmap-list">'

    foreach ($item in $stage.items) {
        $l = $data.legend.($item.status)
        $crit = if ($item.critical) { ' is-critical' } else { '' }

        Add ('      <li class="roadmap-item status-' + $item.status + $crit + '" id="' + $stage.id + '-' + $item.id + '">')
        $statusLine = '<span class="status-tag"><span data-lang="fr">' + (Esc $l.fr) + '</span><span data-lang="en" hidden>' + (Esc $l.en) + '</span></span>'
        if ($item.critical) {
            $statusLine += ' <span class="crit-tag"><span data-lang="fr">à vérifier en priorité</span><span data-lang="en" hidden>check this first</span></span>'
        }
        Add ('        <p class="status-line">' + $statusLine + '</p>')
        Add ('        <p><strong><span data-lang="fr">Touche&nbsp;:</span><span data-lang="en" hidden>Key:</span></strong> ' + (Esc $item.key) + ' — ' + (Bi $item.action) + '</p>')
        Add ('        <p><strong><span data-lang="fr">Attendu&nbsp;:</span><span data-lang="en" hidden>Expected:</span></strong> ' + (Bi $item.expected) + '</p>')
        if ($item.note) {
            Add ('        <p class="roadmap-note"><strong><span data-lang="fr">Retour de jeu&nbsp;:</span><span data-lang="en" hidden>Game feedback:</span></strong> ' + (Bi $item.note) + '</p>')
        }
        Add '      </li>'
    }

    Add '    </ol>'
    Add '  </div>'
    Add ''
}

$body = $sb.ToString()

$navLinks = @()
$n = 0
foreach ($stage in $data.stages) {
    $n++
    $navLinks += '  <a href="#' + $stage.id + '" data-section="' + $stage.id + '"><span data-lang="fr">' + $n + '. ' + (Esc $stage.title.fr) + '</span><span data-lang="en" hidden>' + $n + '. ' + (Esc $stage.title.en) + '</span></a>'
}
$nav = $navLinks -join "`n"

$html = @"
<!doctype html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Feuille de route — Sun Haven Access</title>
<meta name="description" content="Feuille de route de Sun Haven Access : chaque fonctionnalité, dans l'ordre où on la rencontre en jouant, avec son état — à tester, à corriger, à optimiser, ou au point.">
<link rel="canonical" href="https://zenqfr.github.io/SunHavenAccess/roadmap.html">
<meta name="robots" content="index, follow">
<link rel="icon" href="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'%3E%3Ctext y='.85em' font-size='90'%3E%F0%9F%97%BA%3C/text%3E%3C/svg%3E">
<meta property="og:type" content="website">
<meta property="og:title" content="Feuille de route — Sun Haven Access">
<meta property="og:description" content="Chaque fonctionnalité dans l'ordre où on la rencontre en jouant, avec son état.">
<meta property="og:url" content="https://zenqfr.github.io/SunHavenAccess/roadmap.html">
<meta property="og:locale" content="fr_FR">
<meta property="og:locale:alternate" content="en_US">
<meta name="twitter:card" content="summary">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Fredoka:wght@500;600;700&family=Work+Sans:wght@400;500;600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="style.css?v=16">
</head>
<body>
<a class="skip-link" href="#main">
  <span data-lang="fr">Aller au contenu</span><span data-lang="en" hidden>Skip to content</span>
</a>

<nav class="portfolio-bar" aria-label="Mes autres projets">
  <ul>
    <li><a href="https://zenqfr.github.io/">🏠 ZenqFR</a></li>
    <li><a href="index.html">Sun Haven Access</a></li>
    <li aria-current="page"><span data-lang="fr">Feuille de route</span><span data-lang="en" hidden>Roadmap</span></li>
    <li><a href="docs.html"><span data-lang="fr">Documentation</span><span data-lang="en" hidden>Documentation</span></a></li>
  </ul>
  <button type="button" id="lang-toggle" aria-label="Switch language / Changer de langue">English</button>
</nav>

<nav class="quick-nav" aria-label="Navigation rapide">
  <a href="#bilan" data-section="bilan"><span data-lang="fr">Bilan</span><span data-lang="en" hidden>Summary</span></a>
$nav
</nav>

<header>
  <span class="emoji" aria-hidden="true">🗺️</span>
  <h1><span data-lang="fr">Feuille de route</span><span data-lang="en" hidden>Roadmap</span></h1>
  <p class="tagline">
    <span data-lang="fr">Chaque fonctionnalité dans l'ordre où on la rencontre en jouant — et où elle en est.</span>
    <span data-lang="en" hidden>Every feature in the order you meet it while playing — and where it stands.</span>
  </p>
  <span class="status-pill">
    <span data-lang="fr">Mis à jour le $($data.updated) — $done sur $total au point</span>
    <span data-lang="en" hidden>Updated $($data.updated) — $done of $total solid</span>
  </span>
</header>

<p class="intro">
  <span data-lang="fr">Le mod couvre désormais tous les grands systèmes du jeu, mais <strong>à ce stade tout reste à éprouver en conditions réelles</strong> : chaque fonctionnalité listée ici attend d'être essayée. Plus il y a de tests, plus l'accessibilité se précise — un seul retour transforme un « ça devrait marcher » en « ça marche », ou dit exactement ce qu'il faut corriger. Les étapes suivent la progression d'une partie : on vérifie chaque chose au moment où on la rencontre, plutôt que de tout passer en revue d'un bloc.</span>
  <span data-lang="en" hidden>The mod now covers every major system in the game, but <strong>at this stage everything still needs proving in real conditions</strong>: every feature listed here is waiting to be tried. The more testing it gets, the sharper the accessibility becomes — a single report turns "it should work" into "it works", or says exactly what needs fixing. The stages follow a playthrough: you check each thing as you meet it, rather than sweeping the whole list at once.</span>
</p>

<main id="main">

$body
</main>

<footer>
  <p>
    <span data-lang="fr">Un point ne se comporte pas comme annoncé ? Dites-le, ou ouvrez une <a href="https://github.com/ZenqFR/SunHavenAccess/issues">issue</a> — c'est ce qui fait avancer ce mod.</span>
    <span data-lang="en" hidden>Something doesn't behave as described? Say so, or open an <a href="https://github.com/ZenqFR/SunHavenAccess/issues">issue</a> — that's what moves this mod forward.</span>
  </p>
</footer>

<script src="site.js?v=5"></script>
</body>
</html>
"@

[System.IO.File]::WriteAllText((Join-Path $repo 'roadmap.html'), $html, (New-Object System.Text.UTF8Encoding $false))

# --- Copie locale -----------------------------------------------------------------------------
$md = [System.Text.StringBuilder]::new()
function AddMd($line) { [void]$md.AppendLine($line) }

$marks = @{ 'todo' = '[ ]'; 'fix' = '[!]'; 'optimize' = '[~]'; 'ok' = '[x]' }

AddMd '# Feuille de route — Sun Haven Access'
AddMd ''
AddMd 'Généré depuis `roadmap/roadmap.json` par `roadmap/build-roadmap.ps1`. **Ne pas éditer à la main.**'
AddMd ''
AddMd "Mis à jour le $($data.updated) — **$done sur $total au point** ($percent %)."
AddMd ''
AddMd '| Marque | État | Sens | Nombre |'
AddMd '|---|---|---|---|'
foreach ($s in $order) {
    $l = $data.legend.$s
    AddMd "| ``$($marks[$s])`` | $($l.fr) | $($l.hint.fr) | $($counts[$s]) |"
}
AddMd ''
AddMd '⚠️ = à vérifier en priorité.'
AddMd ''

$n = 0
foreach ($stage in $data.stages) {
    $n++
    AddMd "## $n. $($stage.title.fr)"
    AddMd ''
    AddMd $stage.intro.fr
    AddMd ''
    foreach ($item in $stage.items) {
        $star = if ($item.critical) { ' ⚠️' } else { '' }
        AddMd "- $($marks[$item.status]) **$($stage.id)-$($item.id)**$star — touche : $($item.key)"
        AddMd "  - Faire : $($item.action.fr)"
        AddMd "  - Attendu : $($item.expected.fr)"
        if ($item.note) { AddMd "  - Retour de jeu : $($item.note.fr)" }
    }
    AddMd ''
}

[System.IO.File]::WriteAllText((Join-Path $repo 'ROADMAP.md'), $md.ToString(), (New-Object System.Text.UTF8Encoding $false))

Write-Output "roadmap.html et ROADMAP.md régénérés."
foreach ($s in $order) { Write-Output ("  {0,-9} : {1}" -f $data.legend.$s.fr, $counts[$s]) }
Write-Output "  Total     : $total"
