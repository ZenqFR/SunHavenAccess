# SunHavenAccess

Mod d'accessibilité [BepInEx](https://github.com/BepInEx/BepInEx) pour [Sun Haven](https://store.steampowered.com/app/1432860/Sun_Haven/), pensé pour permettre à une personne aveugle ou malvoyante de jouer en autonomie avec un lecteur d'écran, sur le modèle de [stardew-access](https://github.com/stardew-access/stardew-access) pour Stardew Valley.

Tout est vocalisé via [NVDA](https://www.nvaccess.org/) (en direct) ou, à défaut, via [Tolk](https://github.com/dkager/tolk) (SAPI). Le mod est développé en continu — voir [Progression](#-progression--limites-connues) pour l'état actuel exact.

> **Statut : en développement actif, non publié en version stable.** Certaines fonctionnalités listées ci-dessous sont du best-effort pas encore confirmé en conditions réelles — voir la section Progression.

## 📦 Installation

> **Vous découvrez le mod ?** Le guide de démarrage <https://zenqfr.github.io/SunHavenAccess/demarrer.html> vous accompagne de l'installation à votre première heure de jeu, écrit pour un joueur aveugle qui débute.

**[⬇️ Télécharger l'installateur](https://github.com/ZenqFR/SunHavenAccess/releases/latest)** — un seul programme, qui trouve le jeu tout seul et installe BepInEx et le mod. Entièrement pilotable au clavier, chaque contrôle nommé pour le lecteur d'écran, compte rendu dans une zone de texte relisible. N'étant pas signé numériquement, il déclenche l'avertissement SmartScreen de Windows : **Informations complémentaires** puis **Exécuter quand même**.

<details>
<summary><strong>Installation manuelle</strong></summary>

1. Installer [BepInEx 5.4.23.5 (x64, Mono)](https://github.com/BepInEx/BepInEx/releases) dans le dossier du jeu (`Sun Haven/`), et lancer le jeu une fois pour qu'il génère sa structure de dossiers.
2. Copier `SunHavenAccess.dll`, `Tolk.dll` et `nvdaControllerClient64.dll` dans `Sun Haven/BepInEx/plugins/SunHavenAccess/`.
3. Lancer le jeu. Un message vocal confirme le chargement du mod et rappelle la touche d'aide (`F1` par défaut).

</details>

Dans tous les cas, si vous n'entendez rien au lancement : **F11** joue un son Windows indépendant de la synthèse vocale, ce qui distingue « le mod ne s'est pas chargé » de « le lecteur d'écran ne reçoit rien ».

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
<summary><strong>Curseur de case libre</strong></summary>

Le mod ne savait lire qu'une seule case : celle devant le personnage. Explorer une carte inconnue était donc très laborieux — aucun moyen de savoir ce qu'il y a trois cases plus loin sans s'y rendre. Équivalent du *Tile Viewer* de stardew-access.

- Un point de lecture déplaçable **aux flèches n'importe où sur la carte**, indépendamment du personnage. Chaque déplacement annonce le contenu de la case, sa direction et sa distance.
- **S'y rendre** (cheminement automatique) ou **y agir à distance** (le curseur souris est pointé sur la case avant le clic), sans nouvelle touche à retenir : les touches existantes s'appliquent à la case visée quand le curseur est actif.
- Démarre toujours sur la case du joueur, ce qui donne un repère connu — et sert d'autovérification de la conversion.
- Les menus gardent la **priorité absolue** sur les flèches ; le curseur ne les capte que hors menu.

</details>
<details>
<summary><strong>Poser des meubles et des bâtiments</strong></summary>

Poser un objet était l'une des dernières actions entièrement fermées : un aperçu suit la souris et change de teinte — blanc si l'emplacement convient, rouge sinon. Sans la vue, on ne sait ni où vise l'aperçu, ni pourquoi le clic ne fait rien.

- Prendre un objet posable en main annonce le **mode placement**, le nom de l'objet et, s'il dépasse une case, son **emprise au sol**.
- Le **curseur libre sert à viser** : les flèches déplacent l'emplacement visé en même temps que le curseur de lecture, donc on entend à la fois ce qu'il y a sur la case et si l'objet peut y aller.
- **« Emplacement valide » / « invalide » n'est dit qu'aux bascules.** Balayer six cases invalides s'entend une fois, pas six — c'est ce que perçoit un joueur voyant, dont l'œil n'est alerté que par le changement de teinte. L'annonce ne coupe jamais la description de case en cours.
- Une touche dédiée redit l'état complet à la demande.
- Le mod **ne reprogramme pas le placement** : le jeu recalcule déjà la case visée et sa validité à chaque image, et le mod se contente de les lire. Ce qui est annoncé est donc exactement ce que le jeu va faire, sans règle dupliquée qui pourrait diverger — et toutes les variantes (maisons, granges, arbres, papier peint) sont couvertes sans code particulier.

</details>
<details>
<summary><strong>Menus, inventaire, dialogues</strong></summary>

- Navigation clavier maison (flèches directionnelles + Entrée) pour les écrans du jeu qui n'utilisent jamais la sélection clavier native d'Unity (menu principal, options...) — construite en scannant les éléments interactifs visibles à l'écran.
- Lecture des infobulles natives du jeu (nom réel des objets d'inventaire/équipement, pas juste "Item").
- Quantité annoncée AVANT le nom, en une seule phrase ("7, Blé"), pour ne jamais couper le nom de l'objet.
- Emplacements vides et emplacements d'armure décrits explicitement plutôt que silencieux.
- Les 7 onglets du menu principal (Tab) correctement nommés (Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques, Paramètres), et **Ctrl+haut ramène toujours à la barre d'onglets** depuis n'importe quel panneau.
- Bulles de dialogue lues dès le DÉBUT de la ligne, pas à la fin de l'animation machine à écrire.
- Traduction automatique en français des noms techniques d'interface quand le jeu ne fournit aucun texte visible (dictionnaire de plusieurs centaines de termes : actions, lieux, bâtiments...).
- **Navigation directionnelle réelle dans tous les menus** : les flèches suivent la disposition visuelle (ligne/colonne), pas une liste à plat. Gauche/droite restent sur la ligne et **butent en bout avec un bip court** ; haut/bas changent de ligne. Chaque panneau est une zone aux frontières nettes (onglets, équipement, sac à dos, barre d'action), donc on ne « déborde » jamais accidentellement d'une zone à l'autre.
- **Ctrl+flèche** saute volontairement d'une zone à l'autre, selon la disposition réelle : onglets en haut, équipement à gauche, sac à droite, barre d'action en bas.
- **Touches 1 à 0** sur un objet : l'envoie ou le récupère directement du slot de barre d'action correspondant.
- Le bip de bord et la reprise en main des flèches sont tous deux désactivables dans le fichier de config (section `Navigation`).

</details>

<details>
<summary><strong>Confort d'inventaire</strong></summary>

Parcourir 40 emplacements un par un reste lent, même quand tout est bien lu. Ces actions rendent l'inventaire *rapide*, pas seulement lisible.

- **Mode bref** (actif par défaut) : en parcourant, seuls la quantité et le nom de l'objet sont annoncés. Le jeu fusionne nom et description dans un seul texte, ce qui rendait chaque case interminable. Une touche dédiée relit la description complète à la demande, même après la fermeture de l'infobulle. Sans effet en boutique et en artisanat, où le prix et les ingrédients sont indispensables.
- **Tri du sac à dos** : regroupe et compacte les objets, sans toucher à la barre d'action ni à l'équipement. Effet secondaire utile : les emplacements occupés deviennent contigus, donc la navigation devient beaucoup plus courte.
- **Résumé du contenu** : annonce tout ce que contient le sac, regroupé par objet et trié du plus abondant au moins abondant, plus le nombre d'emplacements libres.
- **Rangement dans les coffres proches** : dépose dans les coffres à proximité tout ce dont ils contiennent déjà un exemplaire, et annonce combien de coffres ont été remplis. La méthode équivalente du jeu vise *tous* les coffres chargés, sans filtre de distance — inutilisable à l'aveugle, donc le filtrage par distance est refait ici.

</details>

<details>
<summary><strong>Saisie de texte</strong></summary>

Taper du texte se faisait jusqu'ici **dans le silence total** — ce qui bloquait purement et simplement la création de personnage : impossible de savoir ce qu'on avait tapé comme nom, ni même si la frappe était prise en compte.

- À la prise de focus : le rôle du champ (via son texte d'invite) et son contenu actuel sont annoncés.
- À la frappe : seul le caractère ajouté est annoncé, pas toute la chaîne — sinon saisir un nom deviendrait insupportable.
- À l'effacement : le caractère supprimé est annoncé.
- Les champs masqués (mot de passe) ne sont jamais prononcés à voix haute.
- **Toutes les touches du mod sont suspendues pendant la frappe** : sans ça, taper « p » dans son nom annoncerait la position, « o » l'horloge, « c » ouvrirait le tchat.

Couvre le nom du personnage, le champ de tri de l'artisanat et le tchat, sans code spécifique à chacun.

</details>

<details>
<summary><strong>Coffres et rangement manuel</strong></summary>

- Les emplacements d'un coffre ouvert forment leur propre zone de navigation, avec les mêmes flèches et les mêmes bords que le reste.
- **Ctrl+flèche** passe du coffre au sac à dos et inversement.
- Les touches 1 à 0 sont volontairement inactives sur un emplacement de coffre : elles échangeraient deux cases **du coffre** au lieu d'envoyer l'objet vers la barre d'action.

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
- **Compétences** : niveau et progression ("1234/5678" XP) pour chacune des 5 compétences (combat, agriculture, pêche, minage, exploration).
- **Points de compétence** : touche dédiée pour annoncer le nombre de points disponibles à dépenser dans chaque arbre. Ne remplace pas une vraie navigation dans la grille de nœuds elle-même — voir Progression plus bas pour l'état exact de cette grille.

</details>

<details>
<summary><strong>Création de personnage</strong></summary>

Même problème que la carte du monde : les vignettes d'apparence (`Wish.ClothingImageButton`) ne sont pas de vrais `Selectable`, injoignables au clavier par le système générique — mais le jeu expose déjà tout le nécessaire en données publiques, pas besoin de naviguer la grille visuelle elle-même.

- Race sélectionnée, sa description et sa capacité spéciale annoncées automatiquement à chaque changement.
- Apparence (corps, cheveux, yeux, visage, torse, jambes, tête, queue) : mêmes touches que le scanner (une touche = option dans la catégorie actuelle, Ctrl+la même touche = catégorie précédente/suivante). Chaque changement annonce la catégorie et le nom de l'option choisie.
- Ailes et couleurs pas encore couverts (voir Progression plus bas).

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

Une deuxième touche annonce les festivals de la saison actuelle (nom, jour, description) — le calendrier du jeu (`Wish.CalendarUI`) est une grille de jours purement visuelle sans aucune interaction clavier, cette touche lit directement ses données plutôt que d'essayer de la parcourir.

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
| Virgule / Point | Création de personnage : option précédente / suivante d'apparence (Ctrl+l'une des deux : catégorie précédente/suivante) |
| Z | Annoncer les points de compétence disponibles |
| Point-virgule | Annoncer les festivals de la saison actuelle |
| F9 | Activer/désactiver l'annonce automatique des déplacements |
| F11 | Test sonore (diagnostic) |
| Suppr | Menu des raccourcis (parcourable et modifiable) |
| Pavé 4 / Pavé 6 | Tourner à gauche / à droite sans se déplacer |
| Pavé 5 | Clic gauche (souris) |
| J | Souris directionnelle |
| F8 | Activer/désactiver le bip de visée en pêche |
| Ctrl + : (ou Ctrl + /) | Clic droit (souris) |
| C | Ouvrir le tchat / la console du jeu *(remplace Entrée, qui entrait en conflit avec la validation de menu)* |
| Flèches directionnelles | Se déplacer dans le menu selon la disposition réelle (bip court si on bute sur un bord) |
| Ctrl + flèche | Sauter à la zone voisine (onglets / équipement / sac à dos / barre d'action) |
| 1 à 0 (sur un emplacement) | Envoyer/récupérer l'objet vers/depuis la barre d'action |
| ' (touche ù en AZERTY) | Trier et regrouper le sac à dos |
| `\` (touche * en AZERTY) | Résumé du contenu du sac |
| = | Ranger dans les coffres proches |
| Pavé 0 | Lire la description complète de l'objet annoncé |
| Pavé décimal | Activer/désactiver le curseur de case libre |
| Pavé multiplier | Curseur libre : recentrer sur soi |
| Flèches (curseur libre actif, hors menu) | Déplacer le curseur d'une case |
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
- **Navigation directionnelle du sac à dos/équipement/barre d'action** (voir la carte "Menus, inventaire" plus haut) : voisin le plus proche calculé depuis les positions RectTransform réelles à l'écran — jamais vérifiable sans jeu ouvert, et jamais confirmé si la navigation native du jeu répond aussi (en partie ou en double) aux mêmes flèches sur cet écran.
- **Carte du monde** (voir la carte dédiée plus bas) : correction d'une supposition précédente — `Wish.LocationName` répond bien à la sélection/au clic mais N'HÉRITE PAS de `Selectable`, donc invisible au scan générique de menu du mod ; pas "probablement déjà accessible" comme supposé avant, réellement injoignable au clavier sans les touches dédiées ajoutées. Le reste de l'écran (boutons fermer/changer de région) fonctionne lui via le système générique, ce sont de vrais `Selectable`.
- **Donjon de combat** : numéro d'étage et confirmation de salle nettoyée (voir la carte dédiée plus haut) — risque identifié que l'annonce d'étage se répète si le donjon est composé de plusieurs segments/portes séparés, pas vérifiable sans décompiler la structure de scène réelle.
- **Placement de meubles et bâtiments** (voir la carte dédiée plus haut) : la lecture de `canBePlaced` et `roundedMousePos` se fait par réflexion sur des champs privés/protégés, et le pilotage de la visée repose sur le fait que le jeu relit la position réelle de la souris à chaque image. Deux points à vérifier en jeu : que l'aperçu suit bien le curseur libre, et que le clic pose l'objet sur la case visée et non devant le personnage. Le mode manette (`MouseVisualManager.UsingController`) prend un tout autre chemin de calcul et n'est pas couvert.

</details>

<details>
<summary><strong>Pas encore commencé</strong></summary>

Repérés en explorant systématiquement les classes du jeu (`Wish.*`) au-delà des systèmes déjà couverts :

- **Achievements** : les noms/descriptions viennent des métadonnées Steam (dashboard partenaire, API Steamworks), pas des données locales du jeu — mapping ID→clé Steam à trouver, plus gros chantier que prévu pour un contenu cosmétique.
- ~~**Festivals saisonniers**~~ fait, voir la carte "Horloge, calendrier & météo" plus haut.
- **Animaux de compagnie** (`Wish.PetPanel`) : le panneau lui-même n'est qu'un affichage d'icônes sans donnée riche — probablement déjà couvert pour l'essentiel par la catégorie scanner "animaux et compagnons" existante, pas creusé plus loin.
- **Couleurs en création de personnage** : gérées séparément de la grille d'apparence (voir plus bas), via un vrai `Slider` déjà couvert par Ctrl+flèche gauche/droite (voir la table des touches) — pas revérifié depuis l'ajout de la navigation d'apparence.

</details>

<details>
<summary><strong>Écran de sauvegarde, création de personnage & carte du monde (24/08/2026)</strong></summary>

Trois écrans repérés comme des bloqueurs potentiels (sans lecture d'écran, impossible de commencer/continuer une partie) :

- **Écran de sélection de sauvegarde** : chaque emplacement (`Wish.SavePanel`) lit en un seul résumé le nom du personnage, le jour, les niveaux de compétence et l'argent — champs publics du jeu, lus directement plutôt que de deviner si la lecture générique de menu les aurait trouvés dans le bon ordre.
- **Carte du monde** : les lieux (`Wish.LocationName`) ne sont PAS de vrais `Selectable` (confirmé en décompilation), donc injoignables au clavier par le système générique — touches dédiées ajoutées (voir la carte "Carte du monde" plus haut).
- **Création de personnage — race** : annoncée automatiquement à chaque changement (nom, description, capacité spéciale).
- **Création de personnage — apparence** (corps, cheveux, yeux, visage, torse, jambes, tête, queue) : `Wish.ClothingImageButton` a le même problème que la carte (pas de vrai `Selectable`) — touches dédiées ajoutées, qui pilotent directement les données du jeu (`NewCharacterCreator.CycleLayer`, déjà fournie par le jeu lui-même) plutôt que de naviguer la grille visuelle. "Ailes" non couvert : aucune valeur `ClothingLayer` ne correspond de façon évidente, pas de mapping deviné au hasard. Couleurs non couvertes non plus (voir "pas encore commencé" plus haut).
- **Arbre de compétences** : PAS le même genre de bloqueur que les trois précédents — `Wish.SkillNode` porte un composant `NavigationElement` qui s'ajoute lui-même un `Selectable` nu au démarrage (vu en décompilant `NavigationElement.Start`), donc probablement DÉJÀ repérable par la navigation générique de menu du mod, contrairement à la carte/l'apparence. Jamais vérifié en jeu si ça marche vraiment au clavier (le composant existe, mais rien ne garantit qu'il navigue bien). Un résumé des points de compétence disponibles par arbre a été ajouté en attendant cette confirmation — pas un remplacement pour la navigation dans la grille elle-même si jamais elle s'avère cassée.
- **Lecture des nœuds de compétence** : chaque nœud annonce désormais son nom, son rang (`2 sur 3`, ou « prise » / « non prise » pour un nœud simple), s'il est verrouillé et à quelle condition, puis son effet. Sans ça un nœud n'était qu'une icône sans texte, et l'arbre se parcourait sans qu'on sache jamais sur quoi on était. La validation passe par `submitHandler`, que `SkillNode.OnSubmit` implémente — mais rien de tout ceci n'a été confirmé en jeu.

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
