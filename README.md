# SunHavenAccess

Mod d'accessibilité [BepInEx](https://github.com/BepInEx/BepInEx) pour [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/), pensé pour permettre à une personne aveugle ou malvoyante de jouer en autonomie avec un lecteur d'écran, sur le modèle de [stardew-access](https://github.com/stardew-access/stardew-access) pour Stardew Valley.

Tout est vocalisé via [NVDA](https://www.nvaccess.org/) (en direct) ou, à défaut, via [Tolk](https://github.com/dkager/tolk) (SAPI). Le mod est développé en continu — voir [Progression](#-progression--limites-connues) pour l'état actuel exact.

> **Statut : en développement actif, non publié en version stable.** Certaines fonctionnalités listées ci-dessous sont du best-effort pas encore confirmé en conditions réelles — voir la section Progression.

## 📦 Installation

1. Installer [BepInEx 5.4.23.5 (x64, Mono)](https://github.com/BepInEx/BepInEx/releases) dans le dossier du jeu (`Sun Haven/`), et lancer le jeu une fois pour qu'il génère sa structure de dossiers.
2. Copier `SunHavenAccess.dll`, `Tolk.dll` et `nvdaControllerClient64.dll` dans `Sun Haven/BepInEx/plugins/SunHavenAccess/`.
3. Lancer le jeu. Un message vocal confirme le chargement du mod et rappelle la touche d'aide (`H` par défaut).

`Tolk.dll` et `nvdaControllerClient64.dll` sont des bibliothèques tierces libres, redistribuées ici pour que le mod fonctionne "out of the box" — voir [Remerciements](#-remerciements).

## 🕹️ Fonctionnalités

<details>
<summary><strong>Lecteur d'écran & synthèse vocale</strong></summary>

- NVDA en direct par pilotage natif (`nvdaControllerClient64.dll`), avec repli automatique sur SAPI via Tolk si NVDA n'est pas détecté.
- Aucune dépendance à `System.Speech` ni à une activation COM managée — les deux plantent silencieusement sous le runtime Mono embarqué par Unity.
- Tout le texte lu passe par un nettoyeur qui retire les balises de mise en forme du jeu (couleurs, tailles...) avant vocalisation.

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

> **Toutes vérifiées sans conflit** avec les touches par défaut du jeu (table `Wish.UserSettings.DefaultKeybinds` lue en décompilation) — plusieurs touches ont dû être déplacées le 23/08/2026 en découvrant que F1-F7 sont en fait les émotes du personnage par défaut (F2-F7 utilisées jusque-là déclenchaient donc aussi une émote à chaque pression), qu'Espace est le saut, et que T est un sort.

| Touche par défaut | Action |
|---|---|
| H | Aide (rappelle les touches principales) |
| F10 | Décrire la case devant vous |
| P | Votre position |
| O | Heure, jour, saison, météo |
| N | Personnage proche suivant |
| B | Répéter la dernière annonce |
| F8 | Santé et mana |
| F9 | Activer/désactiver l'annonce automatique des déplacements |
| F11 | Test sonore (diagnostic) |
| F12 | Menu des raccourcis (parcourable et modifiable) |
| Pavé 4 / Pavé 6 | Tourner à gauche / à droite sans se déplacer |
| Pavé 5 | Clic gauche (souris) |
| J | Souris directionnelle |
| Ctrl + : (ou Ctrl + /) | Clic droit (souris) |
| C | Ouvrir le tchat / la console du jeu *(remplace Entrée, qui entrait en conflit avec la validation de menu)* |
| Flèches directionnelles | Naviguer dans un menu |
| Entrée | Valider (clic gauche) *(exige d'avoir déjà sélectionné un élément aux flèches)* |
| Ctrl + Entrée | Action secondaire (clic droit) |
| Page haut / bas | Élément précédent/suivant du scanner |
| Ctrl + Page haut / bas | Catégorie précédente/suivante du scanner |
| Origine | Annoncer l'élément sélectionné par le scanner |
| Ctrl + Origine | Cheminement automatique vers l'élément sélectionné |
| Échap | Annuler un cheminement en cours |
| Fin | Nombre d'éléments trouvés par le scanner |

</details>

## 🚧 Progression & limites connues

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
- **Boutiques et artisanat** : noms d'objets, prix (avec devise correctement annoncée : pièces d'or/tickets/orbes), quantités possédées/requises pour chaque ingrédient ("3/5"), temps de fabrication — passent tous par les mêmes systèmes génériques de lecture d'infobulle/texte que le reste du mod, mais jamais testés spécifiquement sur un écran de boutique ou d'artisanat réel.
- **Courrier** : le contenu complet d'une lettre (message, signature, post-scriptum) est maintenant lu automatiquement à l'ouverture de la boîte aux lettres, sur le même principe que les dialogues — jamais testé en jeu. Les objets éventuellement joints ne sont pour l'instant pas nommés automatiquement (juste leur icône, comme dans les autres menus).

</details>

<details>
<summary><strong>Pas encore développé</strong></summary>

- Pêche et minage (les mini-jeux en temps réel sont un point faible connu même chez stardew-access).
- Donjons/salle de combat spécifiques.
- Carte du monde (fondamentalement visuelle, pas encore de solution accessible envisagée).

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
