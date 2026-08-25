# Installateur de Sun Haven Access

Exécutable graphique qui installe le mod sans que l'utilisateur ait à manipuler des dossiers.

## Pourquoi une interface graphique plutôt qu'un script

Les contrôles WinForms sont de vrais contrôles Win32 : NVDA et JAWS les lisent nativement et de
façon fiable, souvent mieux qu'une sortie console qu'un lecteur d'écran doit suivre au fil de
l'eau. C'est le public visé, donc c'est le critère qui a tranché.

Cible **.NET Framework 4.7.2**, présent d'origine sur tout Windows récent : aucun runtime à
installer avant l'installateur, ce qui serait un comble.

## Choix d'accessibilité

- `AccessibleName` explicite sur chaque contrôle ; le champ de chemin est associé à son étiquette.
- Ordre de tabulation calqué sur l'ordre de lecture.
- **Aucun message éphémère et aucune boîte de dialogue modale** : tout le compte rendu s'écrit
  dans une zone de texte relisible à volonté, et le focus y est déplacé après chaque opération.
  Une barre de progression qui disparaît ne laisse aucune trace pour quelqu'un qui ne l'a pas vue.

## Construire

```powershell
.\build-payload.ps1 -BepInExZip "C:\chemin\vers\BepInEx_x64_5.4.23.5.zip"
```

Le script compile le mod, assemble la charge utile, la compresse en une archive unique, puis
compile l'installateur qui l'embarque. Sans `-BepInExZip`, seul le mod est embarqué (utile pour
tester, mais suppose BepInEx déjà installé chez l'utilisateur).

L'archive plutôt que les fichiers un par un : MSBuild remplace les séparateurs de dossier par des
points dans les noms de ressources, ce qui devient ambigu avec les extensions — reconstruire
l'arborescence à partir de ces noms est fragile. Une archive préserve la structure exacte.

## Avant toute publication

- [ ] Tester sur un dossier de jeu où le mod n'est **pas** déjà installé, sinon le test ne prouve rien.
- [ ] Vérifier la lecture NVDA de chaque contrôle, au clavier uniquement.
- [ ] Inclure `LICENSE-BepInEx.txt` : BepInEx est sous LGPL-2.1, sa licence **doit** accompagner
      toute redistribution.
- [ ] Documenter le passage de l'avertissement SmartScreen : un `.exe` non signé le déclenche, et
      cette boîte de dialogue est un vrai point de friction pour un utilisateur aveugle.
