# SunHavenAccess

Mod d'accessibilité [BepInEx](https://github.com/BepInEx/BepInEx) pour [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/), pensé pour permettre à une personne aveugle ou malvoyante de jouer en autonomie avec un lecteur d'écran, sur le modèle de [stardew-access](https://github.com/stardew-access/stardew-access) pour Stardew Valley.

Tout est vocalisé via [NVDA](https://www.nvaccess.org/) (en direct) ou, à défaut, via [Tolk](https://github.com/dkager/tolk) (SAPI). Le mod est développé en continu — voir [Progression](#-progression--limites-connues) pour l'état actuel exact.

> **Statut : en développement actif, non publié en version stable.** Certaines fonctionnalités listées ci-dessous sont du best-effort pas encore confirmé en conditions réelles — voir la section Progression.

## 📦 Installation

1. Installer [BepInEx 5.4.23.5 (x64, Mono)](https://github.com/BepInEx/BepInEx/releases) dans le dossier du jeu (`Sun Haven/`), et lancer le jeu une fois pour qu'il génère sa structure de dossiers.
2. Copier `SunHavenAccess.dll`, `Tolk.dll` et `nvdaControllerClient64.dll` dans `Sun Haven/BepInEx/plugins/SunHavenAccess/`.
3. Lancer le jeu. Un message vocal confirme le chargement du mod et rappelle la touche d'aide (`F1` par défaut).

`Tolk.dll` et `nvdaControllerClient64.dll` sont des bibliothèques tierces libres, redistribuées ici pour que le mod fonctionne "out of the box" — voir [Remerciements](#-remerciements).

## 🕹️ Fonctionnalités

<details>
<summary><strong>Lecteur d'écran & synthèse vocale</strong></summary>

- NVDA en direct par pilotage natif (`nvdaControllerClient64.dll`), avec repli automatique sur SAPI via Tolk si NVDA n'est pas détecté.
- Aucune dépendance à `System.Speech` ni à une activation COM managée — les deux plantent silencieusement sous le runtime Mono embarqué par Unity.
- Tout le texte lu passe par un nettoyeur qui retire les balises de mise en forme du jeu (couleurs, tailles...) avant vocalisation.
- **Notifications du jeu** lues automatiquement dès qu'elles apparaissent (bulle éphémère en haut à gauche) : boutique fermée, action impossible, et potentiellement bien d'autres messages du jeu jamais rencontrés individuellement — un seul point d'accroche générique les couvre toutes.

</details>

<details>
<summary><strong>Déplacement, curseur de case & terrain</strong></summary>

- La case juste devant le personnage (dans la direction regardée) est décrite à **chaque pas**, systématiquement : culture (arrosée ou non, stade de croissance), terre labourée/arrosée, obstacle physique, type de terrain (herbe, sable, eau...), ou objet interactif — réutilise directement le système de ciblage du jeu, donc toujours cohérent avec ce que la touche d'interaction activerait.
- Tourner sur soi-même sans se déplacer (touches dédiées), avec annonce immédiate de la nouvelle case en face.
- Verbosité des déplacements activable/désactivable (touche dédiée) si l'annonce systématique est trop verbeuse pour certains moments.
- Annonce de la position (coordonnées + direction regardée) à la demande.

</details>

<details>
<summary><strong>Menus, inventaire, dialogues</strong></summary>

- Navigation clavier maison (flèches directionnelles + Entrée) pour les écrans du jeu qui n'utilisent jamais la sélection clavier native d'Unity (menu principal, options...) — construite en scannant les éléments interactifs visibles à l'écran.
- Lecture des infobulles natives du jeu (nom réel des objets d'inventaire/équipement, pas juste "Item").
- Quantité annoncée AVANT le nom, en une seule phrase ("7, Blé"), pour ne jamais couper le nom de l'objet.
- Emplacements vides et emplacements d'armure décrits explicitement plutôt que silencieux.
- Les 7 onglets du menu principal (Tab) correctement nommés (Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques, Paramètres).
- Bulles de dialogue lues dès le DÉBUT de la ligne, pas à la fin de l'animation machine à écrire.
- Traduction automatique en français des noms techniques d'interface quand le jeu ne fournit aucun texte visible (dictionnaire de plusieurs centaines de termes : actions, lieux, bâtiments...).

</details>

<details>
<summary><strong>Agriculture</strong></summary>

- Confirmations vocales non-intrusives à chaque action : labourer, arroser, planter, récolter, arrosoir vide/rempli, culture infusée de mana.
- Description enrichie des cultures (arrosée ou non, jours avant maturité / prête à récolter) et des parcelles labourées sans rien de planté.

</details>

<details>
<summary><strong>Souris directionnelle & simulation de clics</strong></summary>

Beaucoup d'actions de Sun Haven (arroser, labourer, récolter, miner, attaquer) se déclenchent par clic souris, pas par touche d'interaction.

- Touche pour activer/désactiver une souris qui pointe **toujours** vers la case devant le personnage (déplace le vrai curseur Windows).
- Simulation de clic gauche et de clic droit (Ctrl + touche) à la position actuelle de la souris.

</details>

<details>
<summary><strong>Boutiques & artisanat</strong></summary>

- Nom de l'objet, prix (avec la devise correctement annoncée : pièces d'or, tickets ou orbes — les icônes de monnaie ne disparaissent plus silencieusement du texte lu) et quantité restante en boutique.
- En artisanat : nom et quantité possédée/requise de chaque ingrédient ("3/5"), nom de l'objet fabriqué, temps de fabrication.

</details>

<details>
<summary><strong>Courrier</strong></summary>

- Contenu complet d'une lettre (message, signature, post-scriptum) lu automatiquement dès l'ouverture de la boîte aux lettres, sur le même principe que les dialogues.

</details>

<details>
<summary><strong>Quêtes</strong></summary>

Le jeu ouvre son journal de quêtes avec `L` par défaut, mais c'est un écran purement visuel (liste défilante sans navigation clavier native) — les systèmes génériques du mod n'en tirent rien. Cette fonctionnalité lit directement les données de quête du jeu, sans dépendre de cet écran.

- Annonce automatique dès qu'une nouvelle quête est acceptée (nom).
- Annonce automatique quand une quête est rendue/terminée (nom).
- Touche dédiée pour lister toutes les quêtes actives à la demande : nom, description, progression (X/Y objectifs), et où la rendre une fois prête.

</details>

<details>
<summary><strong>Relations & compétences</strong></summary>

Deux écrans du jeu (relations avec les PNJ, niveaux de compétence) n'affichent leur information que visuellement (cœurs remplis, barres de progression) sans texte natif équivalent — ces touches recalculent l'équivalent en mots directement depuis les données du jeu.

- **Relations** : cœurs (sur 10, 15 ou 20 selon le statut simple/en couple/marié) pour chaque PNJ romançable avec qui une relation a été nouée, triés du plus investi au moins investi.
- **Compétences** : niveau et progression ("1234/5678" XP) pour chacune des 5 compétences (combat, agriculture, pêche, minage, exploration). Ne couvre pas encore l'arbre de compétences lui-même (dépense de points sur une grille de nœuds) — chantier à part entière, pas encore commencé.

</details>

<details>
<summary><strong>Carte du monde</strong></summary>

Chaque lieu de la carte répond bien à la sélection/au clic dans le jeu, mais son composant (`Wish.LocationName`) n'hérite PAS de `Selectable` — invisible au scan générique du mod, donc injoignable au clavier sans ces touches dédiées, même si le reste de l'écran (fermer, changer de région) fonctionne déjà via le système générique.

- Touches dédiées pour parcourir un par un les lieux de la région actuellement affichée.
- Chaque lieu annonce son nom et sa description dès qu'il est ouvert — centre et surligne aussi la carte, comme un clic.

</details>

<details>
<summary><strong>Pêche</strong></summary>

La pêche est un mini-jeu en temps réel — un vrai point faible d'accessibilité même chez stardew-access. En décompilant le mini-jeu (`Wish.Bobber`), découverte que contrairement à Stardew Valley (deux éléments mobiles indépendants à suivre), ici une seule jauge oscille TOUTE SEULE en aller-retour automatique : il suffit d'appuyer au bon moment pendant qu'elle traverse une zone gagnante fixe.

- Annonce quand un poisson mord (pour réagir à temps).
- Annonce du résultat de chaque pression (touché / manqué / poisson échappé).
- **Bip continu de visée** : la hauteur du son varie avec la distance à la zone gagnante (grave = loin, aigu = proche, double bip distinctif quand on est dedans) — pour viser au son plutôt qu'à l'aveugle. Touche dédiée pour le désactiver si le son ne convient pas. **Fonctionnalité toute nouvelle, jamais entendue en conditions réelles** (fréquences et tempo choisis au jugé) — à tester en priorité.

</details>

<details>
<summary><strong>Minage & coupe de bois</strong></summary>

- Annonce quand l'outil en main n'est pas assez puissant pour un rocher/arbre donné (ex. pioche en bois sur un "heavystone") — le jeu ne donne autrement AUCUN indice, ni visuel ni sonore, quand un coup ne fait rien : on frapperait indéfiniment sans savoir que c'est l'outil en cause.
- Confirmation vocale quand un rocher se brise ou qu'un arbre est abattu.

</details>

<details>
<summary><strong>Donjon de combat</strong></summary>

Une succession de salles à vider de tous leurs ennemis pour avancer d'étage en étage — jusqu'ici silencieux au-delà du combat lui-même :

- Annonce du numéro d'étage à l'entrée d'une salle.
- Confirmation vocale quand une salle est nettoyée et que la porte s'ouvre.
- La récompense de fin de parcours passe par le système de dialogue habituel, déjà lu automatiquement.

</details>

<details>
<summary><strong>Scanner par catégories (façon Object Tracker de stardew-access)</strong></summary>

Repère tout ce qui se trouve à proximité, par catégorie, trié du plus proche au plus loin :

**Personnages · Plantations · Ressources (dont minerais) · Bâtiments et portails (avec destination annoncée) · Animaux et compagnons · Ennemis · Mobilier et rangement (coffres, boîte aux lettres, lit)**

- Parcourir les éléments trouvés / changer de catégorie (les catégories vides sont automatiquement sautées).
- Annoncer l'élément sélectionné, ou le nombre total trouvé.
- **Cheminement automatique** vers l'élément sélectionné : le personnage marche seul jusqu'à la destination (A* maison, calculé à la demande, en tenant compte des vrais obstacles physiques de la carte). Si la destination exacte est bloquée, avance quand même jusqu'au point le plus proche atteignable plutôt que de ne rien faire.

</details>

<details>
<summary><strong>Combat</strong></summary>

- Santé restante annoncée à chaque coup reçu, avec alerte progressive (santé basse, critique, à terre).
- Entrée/sortie de combat annoncée.
- Défaite d'un ennemi annoncée.
- Mort du personnage annoncée.
- Santé et mana consultables à la demande.

</details>

<details>
<summary><strong>Horloge, calendrier, météo</strong></summary>

Une touche annonce l'heure, le jour de la semaine, le jour du mois, la saison, l'année et la météo actuelle (pluie, neige, canicule, brouillard, vent) — rien de tout ça n'était accessible autrement que visuellement.

</details>

<details>
<summary><strong>Barre d'action</strong></summary>

Changer d'objet en main (touches 1-0, molette...) annonce son nom et sa quantité.

</details>

<details>
<summary><strong>Raccourcis clavier — tous personnalisables</strong></summary>

Chaque touche peut être changée directement dans `BepInEx/config/com.kleitz.sunhavenaccess.cfg`, ou en jeu via un menu vocal dédié qui liste tous les raccourcis, ce qu'ils font, et permet de les réassigner à la volée.

> **Vérifiées sans conflit** avec les touches par défaut du jeu (table `Wish.UserSettings.DefaultKeybinds` lue en décompilation) — plusieurs touches ont dû être déplacées le 23/08/2026 en découvrant que F1-F7 sont en fait les émotes du personnage par défaut (F2-F7 utilisées jusque-là déclenchaient donc aussi une émote à chaque pression), qu'Espace est le saut, et que T est un sort. **Exception assumée** : la touche d'aide a ensuite été remise sur `F1` sur demande explicite de l'utilisateur malgré ce conflit connu — elle déclenche donc aussi l'émote 1 du personnage à chaque pression. Second conflit repéré le même jour en travaillant sur les quêtes : `K` était aussi la touche par défaut du menu Compétences (`Button.Skills`) — déplacée sur `F8`, libre des deux côtés.

| Touche par défaut | Action |
|---|---|
| F1 | Aide (rappelle les touches principales) — ⚠️ déclenche aussi l'émote 1 du jeu |
| F10 | Décrire la case devant vous |
| P | Votre position |
| O | Heure, jour, saison, météo |
| N | Personnage proche suivant |
| B | Répéter la dernière annonce |
| H | Santé et mana |
| G | Annoncer les quêtes actives |
| V | Annoncer les relations avec les PNJ |
| U | Annoncer les niveaux de compétence |
| X / Y | Carte du monde : lieu précédent / suivant (carte ouverte uniquement) |
| F9 | Activer/désactiver l'annonce automatique des déplacements |
| F11 | Test sonore (diagnostic) |
| F12 | Menu des raccourcis (parcourable et modifiable) |
| Pavé 4 / Pavé 6 | Tourner à gauche / à droite sans se déplacer |
| Pavé 5 | Clic gauche (souris) |
| J | Souris directionnelle |
| F8 | Activer/désactiver le bip de visée en pêche |
| Ctrl + : (ou Ctrl + /) | Clic droit (souris) |
| C | Ouvrir le tchat / la console du jeu *(remplace Entrée, qui entrait en conflit avec la validation de menu)* |
| Flèches directionnelles | Naviguer dans un menu |
| Entrée | Valider (clic gauche) *(exige d'avoir déjà sélectionné un élément aux flèches)* |
| Ctrl + Entrée | Action secondaire (clic droit) |
| Ctrl + Tab / Ctrl + Maj + Tab | Changer d'onglet directement dans le menu principal (Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques, Paramètres) |
| Ctrl + Flèche gauche / droite | Diminuer/augmenter un curseur (slider) sélectionné (ex. couleurs en création de personnage) |
| Page haut / bas | Élément précédent/suivant du scanner |
| Ctrl + Page haut / bas | Catégorie précédente/suivante du scanner |
| Origine | Annoncer l'élément sélectionné par le scanner |
| Ctrl + Origine | Cheminement automatique vers l'élément sélectionné |
| Échap | Annuler un cheminement en cours |
| Fin | Nombre d'éléments trouvés par le scanner |

</details>

## 🚧 Progression & limites connues

La plupart des systèmes majeurs du jeu ont désormais au moins un premier niveau de couverture. Beaucoup d'éléments ci-dessous restent malgré tout à confirmer en conditions réelles (voir le détail) ; quelques écrans repérés en explorant le jeu au-delà des fonctionnalités déjà couvertes n'ont, eux, pas encore été commencés du tout.

<details>
<summary><strong>Développé et fonctionnel (confirmé en jeu)</strong></summary>

- Boucle du mod stable sur toute une session de jeu.
- Synthèse vocale (NVDA + repli Tolk).
- Curseur de case, dialogues, menus/inventaire/tooltips, agriculture, souris directionnelle, scanner (une fois le bug de filtrage de scène corrigé), navigation clavier des menus, touche du tchat.

</details>

<details>
<summary><strong>Développé, pas encore confirmé en conditions réelles</strong></summary>

Je (l'IA qui développe ce mod) n'ai pas d'yeux ni d'oreilles pour tester en jeu — je décompile le code du jeu, raisonne dessus, et déploie, mais la validation finale vient toujours d'un retour humain :

- Description du TYPE de terrain hors zone cultivable (herbe, sable, pierre...) — la présence d'eau est fiable (donnée de jeu directe), le reste est déduit du nom brut des tuiles et peut nécessiter des ajustements de traduction.
- Cheminement automatique en présence d'obstacles complexes (le masque de collision est déduit dynamiquement, pas testé sur tous les types de terrain).
- **Minage & coupe de bois** : annonce d'outil trop faible et confirmation de casse (voir la carte dédiée plus haut) — patch Harmony sur les méthodes internes du jeu, jamais déclenché en conditions réelles.
- **Notifications génériques du jeu** : un seul patch couvre potentiellement énormément de messages différents (boutique fermée, action impossible...) jamais rencontrés individuellement en décompilation — l'ampleur réelle de ce que ça couvre ne pourra se confirmer qu'à l'usage.
- **Boutiques et artisanat** : noms d'objets, prix (avec devise correctement annoncée : pièces d'or/tickets/orbes), quantités possédées/requises pour chaque ingrédient ("3/5"), temps de fabrication — passent tous par les mêmes systèmes génériques de lecture d'infobulle/texte que le reste du mod, mais jamais testés spécifiquement sur un écran de boutique ou d'artisanat réel.
- **Courrier** : le contenu complet d'une lettre (message, signature, post-scriptum) est maintenant lu automatiquement à l'ouverture de la boîte aux lettres, sur le même principe que les dialogues — jamais testé en jeu. Les objets éventuellement joints ne sont pour l'instant pas nommés automatiquement (juste leur icône, comme dans les autres menus).
- **Pêche** : annonce touche/résultat + bip continu de visée, voir la carte dédiée plus haut — le bip est la fonctionnalité la moins certaine de tout le mod (fréquences/tempo jamais entendus en jeu).
- **Quêtes** : acceptation/rendu automatiquement annoncés, touche dédiée pour lister les quêtes actives (voir la carte dédiée plus haut) — tous les champs lus sont publics et déjà remplis par le jeu (`QuestPanel.questDescription`/`questProgress`/`questCompleteTMP`), mais jamais entendu en conditions réelles. Le format exact de `questProgress` (texte de progression détaillé par objectif) n'a pas pu être confirmé sans décompiler chaque type de `QuestRequirement` individuellement — inclus tel quel, peut être incomplet ou redondant avec la description.
- **Relations & compétences** (voir la carte dédiée plus haut) : calcul des cœurs/niveaux jamais entendu en jeu. Le nombre exact de cœurs affichés visuellement par statut (10/15/20) est déduit des plafonds de points en décompilation (50/75/100, 5 points/cœur) — cohérent avec le code, pas observé à l'écran.
- **Carte du monde** (voir la carte dédiée plus haut) : la région actuellement affichée (`Map.townType`) et la liste de lieux qui va avec sont lues par réflexion (5 champs privés séparés, un par région) — jamais navigué en jeu pour confirmer que la bonne liste sort à chaque fois qu'on change de région à l'écran.
- **Carte du monde** (voir la carte dédiée plus bas) : correction d'une supposition précédente — `Wish.LocationName` répond bien à la sélection/au clic mais N'HÉRITE PAS de `Selectable`, donc invisible au scan générique de menu du mod ; pas "probablement déjà accessible" comme supposé avant, réellement injoignable au clavier sans les touches dédiées ajoutées. Le reste de l'écran (boutons fermer/changer de région) fonctionne lui via le système générique, ce sont de vrais `Selectable`.
- **Donjon de combat** : numéro d'étage et confirmation de salle nettoyée (voir la carte dédiée plus haut) — risque identifié que l'annonce d'étage se répète si le donjon est composé de plusieurs segments/portes séparés, pas vérifiable sans décompiler la structure de scène réelle.

</details>

<details>
<summary><strong>Pas encore commencé</strong></summary>

Repérés en explorant systématiquement les classes du jeu (`Wish.*`) au-delà des systèmes déjà couverts, pas encore attaqués :

- **Arbre de compétences** (`Wish.SkillTree`/`SkillTreeButton`, touche `K` par défaut du jeu) : grille 2D de nœuds à débloquer avec des points de compétence — contrairement aux relations/niveaux (juste des chiffres à annoncer), ici il faudrait une vraie navigation spatiale dans la grille, un chantier bien plus lourd que le reste du mod jusqu'ici.
- **Création de personnage** (`Wish.NewCharacterCreator`) : écran d'apparence par grille de vignettes (corps, cheveux, yeux, visage, torse, jambes, tête, ailes, queue, couleurs) — inconnu si les systèmes génériques existants (FocusReader) en tirent déjà quelque chose d'utilisable ou pas, jamais vérifié. Potentiellement un vrai bloqueur : sans lecture d'écran ici, impossible de commencer une partie du tout.
- **Sauvegarde/chargement de partie** (`Wish.SavePanel`/`LoadCharacterMenu`) : même remarque — jamais vérifié si la liste des sauvegardes est lisible via les systèmes génériques.
- **Achievements, festivals saisonniers, animaux de compagnie** (`Wish.AchievementProgressManager`, `SeasonEventUI`, `PetPanel`) : contenu secondaire, pas encore regardé du tout.

</details>

<details>
<summary><strong>Premiers pas côté sauvegarde & création de personnage (24/08/2026)</strong></summary>

Deux vrais bloqueurs potentiels (sans lecture d'écran ici, impossible de commencer à jouer du tout) — pas résolus entièrement, mais un premier pas concret sur chacun :

- **Écran de sélection de sauvegarde** : chaque emplacement de sauvegarde (`Wish.SavePanel`) lit maintenant, en un seul résumé, le nom du personnage, le jour, les niveaux de compétence et l'argent — tous des champs publics du jeu, lus directement plutôt que de deviner si la lecture générique de menu les aurait trouvés dans le bon ordre.
- **Création de personnage** : la race sélectionnée, sa description et sa capacité spéciale sont maintenant annoncées automatiquement à chaque changement. Le reste de l'écran (apparence : corps, cheveux, yeux, visage, torse, jambes, tête, ailes, queue, couleurs — une grille de vignettes) n'est **pas** encore couvert : une vraie navigation spatiale dans cette grille serait nécessaire, jamais testée sans retour humain, donc pas commencée pour l'instant.

</details>

## 🙏 Remerciements

- [stardew-access](https://github.com/stardew-access/stardew-access) — la référence dont ce mod s'inspire directement (conventions de touches du scanner, philosophie générale).
- [Tolk](https://github.com/dkager/tolk) par Davy Kager — pilote de synthèse vocale multi-lecteurs d'écran.
- [NV Access](https://www.nvaccess.org/) — NVDA et son SDK de contrôle (`nvdaControllerClient64.dll`), librement redistribuable pour ce type d'usage.
- [BepInEx](https://github.com/BepInEx/BepInEx) — le framework de modding qui rend tout ça possible.

## 🛠️ Compiler soi-même

Projet .NET Framework 4.7.2 (SDK-style). Nécessite les DLL du jeu et de BepInEx en référence (chemins configurables en haut de `SunHavenAccess.csproj`, `Private=false` partout — le jeu et BepInEx les fournissent déjà à l'exécution).

```bash
dotnet build -c Release
```

Une cible MSBuild (`DeployToGame`) copie automatiquement le résultat dans `BepInEx/plugins/SunHavenAccess/` après chaque build.
