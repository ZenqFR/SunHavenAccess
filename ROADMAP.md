# Feuille de route — Sun Haven Access

Généré depuis `roadmap/roadmap.json` par `roadmap/build-roadmap.ps1`. **Ne pas éditer à la main.**

Mis à jour le 2026-08-26 — **6 sur 87 au point** (7 %).

| Marque | État | Sens | Nombre |
|---|---|---|---|
| `[ ]` | À tester | jamais essayé en jeu | 81 |
| `[!]` | À corriger | essayé, ne marche pas | 0 |
| `[~]` | À optimiser | marche, mais perfectible | 0 |
| `[x]` | Au point | essayé, rien à redire | 6 |

⚠️ = à vérifier en priorité.

## 1. Avant de lancer le jeu

Installer, et vérifier que le mod se charge. Si ça échoue ici, rien d'autre ne peut marcher.

- [x] **avant-installateur-clavier** — touche : —
  - Faire : Lancer l'installateur et parcourir tous les contrôles au clavier.
  - Attendu : Chaque contrôle est nommé, l'ordre de tabulation suit la lecture, et le compte rendu se relit aux flèches.
- [x] **avant-installateur-smartscreen** — touche : —
  - Faire : Passer l'avertissement SmartScreen de Windows.
  - Attendu : « Informations complémentaires » puis « Exécuter quand même » sont trouvables au lecteur d'écran.
- [x] **avant-installateur-detection** — touche : —
  - Faire : Regarder si le jeu est trouvé tout seul.
  - Attendu : Le chemin de Sun Haven est prérempli sans rien saisir.
- [ ] **avant-installateur-mise-a-jour** — touche : —
  - Faire : Relancer l'installateur alors que le mod est déjà installé.
  - Attendu : Le bouton principal annonce « Mettre à jour le mod » et non « Installer ». Après une désinstallation, il redevient « Installer ».
- [x] **avant-chargement** ⚠️ — touche : —
  - Faire : Lancer Sun Haven.
  - Attendu : Un message vocal annonce que le mod est chargé et rappelle la touche d'aide.
- [x] **avant-test-son** — touche : F11
  - Faire : Appuyer sur F11.
  - Attendu : Un son Windows se fait entendre, indépendamment de la synthèse vocale — pour distinguer « mod pas chargé » de « lecteur d'écran muet ».
- [ ] **avant-aide** — touche : F1
  - Faire : Appuyer sur F1, parcourir les rubriques aux flèches, puis fermer avec Échap ou F1.
  - Attendu : Un menu d'aide s'ouvre et annonce le nombre de rubriques. Les flèches passent d'une rubrique à la suivante, une seule est lue à la fois, et un bip signale les extrémités. Origine et Fin vont à la première et à la dernière. F1 est aussi une émote du jeu, le personnage fera donc un geste en plus.
  - Retour de jeu : L'ancienne version, qui lisait les quarante touches d'un bloc, était validée mais inexploitable en pratique. Remplacée par ce menu — à revérifier.
- [x] **avant-menu-raccourcis** — touche : Suppr
  - Faire : Ouvrir le menu des raccourcis, le parcourir, réassigner une touche.
  - Attendu : Chaque raccourci est annoncé avec son libellé et sa touche, et se change sans quitter le jeu.

## 2. Créer son personnage

Le tout premier écran d'une nouvelle partie. Il était totalement muet avant le mod : impossible de commencer à jouer.

- [ ] **personnage-menu-principal** — touche : Flèches, Entrée
  - Faire : Naviguer dans le menu principal et l'écran de sauvegarde.
  - Attendu : Les boutons sont nommés, et chaque sauvegarde annonce son résumé.
- [ ] **personnage-race** — touche : Flèches
  - Faire : Choisir une race.
  - Attendu : Chaque race est annoncée au passage.
- [ ] **personnage-apparence** — touche : Virgule / Point
  - Faire : Parcourir les options d'apparence, puis changer de catégorie avec Ctrl.
  - Attendu : Chaque option est annoncée ; Ctrl passe à la catégorie suivante. Les ailes ne sont pas couvertes.
- [ ] **personnage-saisie-nom** ⚠️ — touche : —
  - Faire : Taper le nom du personnage.
  - Attendu : Chaque caractère tapé est annoncé, l'effacement aussi. Aucune touche du mod ne se déclenche pendant la frappe.

## 3. Premiers pas dans le monde

Savoir où on est et ce qu'on a devant soi. C'est le socle : tout le reste s'appuie dessus.

- [ ] **premiers-pas-case-devant-auto** ⚠️ — touche : —
  - Faire : Marcher.
  - Attendu : La case devant vous est décrite à chaque pas : terrain, culture, obstacle ou objet.
- [ ] **premiers-pas-case-devant** — touche : F10
  - Faire : Appuyer sur F10 à l'arrêt.
  - Attendu : La même description, à la demande.
- [ ] **premiers-pas-position** — touche : P
  - Faire : Appuyer sur P.
  - Attendu : Coordonnées et direction regardée.
- [ ] **premiers-pas-tourner** — touche : Pavé 4 / Pavé 6
  - Faire : Tourner sur place à gauche puis à droite.
  - Attendu : Le personnage pivote sans bouger, et la nouvelle case en face est annoncée.
- [ ] **premiers-pas-repeter** — touche : B
  - Faire : Appuyer sur B après n'importe quelle annonce.
  - Attendu : La dernière chose annoncée est répétée.
- [ ] **premiers-pas-verbosite** — touche : F9
  - Faire : Couper puis remettre la verbosité, en marchant à chaque fois.
  - Attendu : Coupée, plus d'annonce automatique en marchant ; remise, elles reviennent.
- [ ] **premiers-pas-horloge** — touche : O
  - Faire : Appuyer sur O.
  - Attendu : Heure, jour, saison et météo.
- [ ] **premiers-pas-statut** — touche : H
  - Faire : Appuyer sur H.
  - Attendu : Santé, énergie et argent.
- [ ] **premiers-pas-notifications** — touche : —
  - Faire : Provoquer un message du jeu (entrer dans une boutique fermée, action impossible).
  - Attendu : La bulle éphémère en haut à gauche est lue automatiquement.

## 4. Son sac et son équipement

La navigation dans les menus a été entièrement refaite et n'a jamais été confirmée en jeu. Le point à surveiller : qu'une pression ne provoque pas deux déplacements.

- [ ] **sac-onglets** — touche : Tab, puis Ctrl+Tab
  - Faire : Ouvrir le menu principal et passer d'un onglet à l'autre.
  - Attendu : Les 7 onglets sont nommés dans l'ordre : Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques, Paramètres.
- [ ] **sac-nav-directionnelle** ⚠️ — touche : Flèches
  - Faire : Dans le sac, se déplacer ligne par ligne et colonne par colonne.
  - Attendu : Les flèches suivent la disposition visuelle réelle. Gauche/droite butent en bout de ligne avec un bip. Une pression = un seul déplacement.
- [ ] **sac-infobulles** ⚠️ — touche : Flèches
  - Faire : Passer sur des objets du sac et de l'équipement.
  - Attendu : Le NOM et la DESCRIPTION sont lus, pas seulement la quantité. Les emplacements vides le disent.
- [ ] **sac-zones** — touche : Ctrl + flèches
  - Faire : Sauter entre les zones : onglets, équipement, sac, barre d'action.
  - Attendu : Chaque zone est atteignable, et Ctrl+haut ramène TOUJOURS à la barre d'onglets, depuis n'importe quel panneau.
- [ ] **sac-barre-action** — touche : 1 à 0
  - Faire : Sur un objet du sac, appuyer sur un chiffre.
  - Attendu : L'objet part vers le slot de barre d'action correspondant — et le même geste le récupère.
- [ ] **sac-description-complete** — touche : Pavé 0
  - Faire : Après l'annonce d'un objet, demander la description complète.
  - Attendu : La description longue est relue en entier.
- [ ] **sac-confort-sac** — touche : Apostrophe, Antislash, Égal
  - Faire : Trier le sac, demander son résumé, ranger dans les coffres proches.
  - Attendu : Les trois actions se font et sont confirmées vocalement.
- [ ] **sac-coffres** — touche : Flèches
  - Faire : Ouvrir un coffre et naviguer dedans.
  - Attendu : Le contenu du coffre est une zone à part, avec ses propres frontières.

## 5. Travailler sa ferme

La boucle quotidienne : labourer, planter, arroser, récolter.

- [ ] **ferme-energie-alertes** ⚠️ — touche : —
  - Faire : Miner ou labourer jusqu'à épuiser l'énergie.
  - Attendu : Une alerte à la moitié, au quart, presque à sec, puis à l'épuisement. Chacune n'est dite qu'une fois, et n'interrompt pas l'annonce en cours.
- [ ] **ferme-energie-pas-de-spam** — touche : —
  - Faire : Sous un seuil, consommer et regagner un peu d'énergie plusieurs fois de suite.
  - Attendu : L'alerte ne se répète pas à chaque oscillation. Elle ne revient qu'après une vraie remontée au-dessus du seuil.
- [ ] **ferme-energie-lendemain** — touche : —
  - Faire : Épuiser l'énergie, dormir, puis retravailler le lendemain.
  - Attendu : Les alertes recommencent normalement au fil de la nouvelle journée — l'état de la veille ne les bloque pas.
- [ ] **ferme-agriculture** ⚠️ — touche : —
  - Faire : Labourer, arroser, planter, récolter.
  - Attendu : Chaque action est confirmée, et l'arrosoir annonce quand il est vide.
- [ ] **ferme-cultures** — touche : F10
  - Faire : Se placer devant une culture à différents stades.
  - Attendu : Le stade de croissance et l'état d'arrosage sont annoncés.
- [ ] **ferme-souris-directionnelle** — touche : J
  - Faire : Activer la souris directionnelle, puis se déplacer et tourner.
  - Attendu : Le pointeur suit toujours la case devant le personnage.
- [ ] **ferme-clic-monde** — touche : Pavé 5 / Deux-points
  - Faire : Simuler un clic gauche puis un clic droit dans le monde.
  - Attendu : L'action correspondante se déclenche sur la case visée.

## 6. S'occuper des animaux

Tout nouveau, jamais testé. Le point à vérifier avant tout : que le compte annoncé corresponde à la réalité.

- [ ] **animaux-troupeau** — touche : Pavé moins
  - Faire : Dans un enclos avec des animaux, demander le bilan.
  - Attendu : Effectif, combien à nourrir, combien à caresser, combien de produits au sol. Les noms sont cités jusqu'à trois animaux.
- [ ] **animaux-troupeau-exact** ⚠️ — touche : Pavé moins
  - Faire : Nourrir et caresser un animal, puis redemander le bilan.
  - Attendu : Le compte diminue en conséquence, et correspond à la réalité.
- [ ] **animaux-animal-scanner** — touche : Page haut/bas puis Origine
  - Faire : Trouver les animaux au scanner.
  - Attendu : Chaque animal annonce son état, pas seulement son nom.

## 7. Explorer et se repérer

Le curseur libre est l'outil qui ouvre l'exploration. Son premier test vérifie la conversion entre cases et monde : si elle est fausse, tout le reste du curseur l'est au même endroit.

- [ ] **explorer-curseur-autoverif** ⚠️ — touche : Pavé décimal
  - Faire : Activer le curseur, puis le déplacer d'une case dans la direction regardée.
  - Attendu : À l'activation il décrit la case où vous vous tenez. Après un cran, il doit dire exactement ce que dit F10. Si ça diverge, la conversion est fausse et le correctif tient dans une seule fonction.
- [ ] **explorer-curseur-deplacement** — touche : Flèches
  - Faire : Déplacer le curseur dans les quatre directions.
  - Attendu : Chaque déplacement annonce le contenu de la case, sa direction et sa distance.
- [ ] **explorer-curseur-menus** ⚠️ — touche : Flèches
  - Faire : Ouvrir un menu pendant que le curseur est actif, puis utiliser les flèches.
  - Attendu : Les flèches pilotent le MENU, pas le curseur. C'est le piège qui a déjà cassé la navigation deux fois.
- [ ] **explorer-curseur-bord** — touche : Flèches
  - Faire : Pousser le curseur jusqu'au bord de la carte chargée.
  - Attendu : Un bip court signale la butée, le curseur ne sort pas.
- [ ] **explorer-curseur-clic** — touche : Pavé 5
  - Faire : Curseur actif, agir sur une case à distance.
  - Attendu : L'action porte sur la case visée, pas sur celle devant le personnage.
- [ ] **explorer-curseur-recentrer** — touche : Pavé multiplier
  - Faire : Après avoir éloigné le curseur, le recentrer.
  - Attendu : Le curseur revient sur votre case sans quitter le mode.
- [ ] **explorer-scanner-categories** — touche : Page haut / Page bas
  - Faire : Changer de catégorie du scanner.
  - Attendu : Les 7 catégories défilent et sont nommées.
- [ ] **explorer-scanner-parcours** — touche : Origine / Fin
  - Faire : Parcourir les éléments de la catégorie courante.
  - Attendu : Chaque élément est annoncé avec sa direction et sa distance.
- [ ] **explorer-scanner-trajet** ⚠️ — touche : Ctrl + Origine
  - Faire : Lancer le cheminement vers l'élément annoncé, ou vers la case du curseur.
  - Attendu : Le personnage s'y rend seul. En cas d'échec, la raison est dite (trop loin, chemin bloqué).
- [ ] **explorer-carte** — touche : X / Y
  - Faire : Ouvrir la carte du monde et parcourir les lieux.
  - Attendu : Les lieux sont nommés un par un.

## 8. Le village et ses habitants

Parler, acheter, recevoir du courrier, suivre ses quêtes.

- [ ] **village-npc-proche** — touche : N
  - Faire : Appuyer sur N plusieurs fois près de personnages.
  - Attendu : On passe d'un personnage proche au suivant, avec nom, direction et distance.
- [ ] **village-dialogues** ⚠️ — touche : —
  - Faire : Parler à un personnage, y compris avec des choix de réponse.
  - Attendu : Les lignes sont lues dès le début, sans attendre l'animation. Les choix sont parcourables.
- [ ] **village-boutique** — touche : Flèches
  - Faire : Acheter et vendre chez un marchand.
  - Attendu : Noms, prix et devise (or, tickets, orbes) sont annoncés correctement.
- [ ] **village-artisanat** — touche : Flèches
  - Faire : Fabriquer un objet, y compris en cherchant dans le champ de tri.
  - Attendu : Recettes, ingrédients et manques sont annoncés ; la frappe dans le champ de recherche est vocalisée.
- [ ] **village-courrier** — touche : —
  - Faire : Ouvrir une lettre dans la boîte aux lettres.
  - Attendu : Le contenu complet est lu : message, signature, post-scriptum.
- [ ] **village-panneau-taches** ⚠️ — touche : Pavé Entrée
  - Faire : S'approcher d'un panneau d'affichage SANS l'ouvrir, et demander les tâches.
  - Attendu : Les deux tâches du jour sont annoncées avec leur nom, leur énoncé et leur récompense. C'est le point à vérifier : les tâches doivent être lisibles avant même d'avoir ouvert le panneau.
- [ ] **village-panneau-acceptee** — touche : Pavé Entrée
  - Faire : Accepter une tâche, puis redemander.
  - Attendu : Elle passe de « à prendre » à « déjà acceptée », immédiatement et sans recharger la partie.
- [ ] **village-panneau-loin** — touche : Pavé Entrée
  - Faire : S'éloigner du panneau et réessayer ; puis essayer dans une ville sans panneau à proximité.
  - Attendu : « Aucun panneau d'affichage à proximité. » Le rayon doit correspondre à « je suis devant », sans capter le panneau d'une autre place.
- [ ] **village-panneau-baraquements** — touche : Pavé Entrée
  - Faire : Essayer au panneau des baraquements.
  - Attendu : Une seule commission est annoncée, pas deux tâches.
- [ ] **village-quetes** — touche : G
  - Faire : Accepter une quête, la rendre, puis lister les quêtes actives.
  - Attendu : Acceptation et rendu sont annoncés d'eux-mêmes ; la liste donne nom, description et progression.
- [ ] **village-relations** — touche : V
  - Faire : Appuyer sur V.
  - Attendu : Les cœurs de chaque personnage romançable, avec le statut (en couple, marié).
- [ ] **village-festivals** — touche : Point-virgule
  - Faire : Appuyer sur point-virgule.
  - Attendu : Les festivals de la saison en cours sont listés.

## 9. Les paquets à compléter

Musée, autel de Dynus, aquarium : les collections qu'on remplit sur toute une partie. Tout nouveau, jamais testé. Le point à vérifier avant tout : que l'objet annoncé soit bien celui que l'emplacement réclame.

- [ ] **paquets-paquet-manque** ⚠️ — touche : Pavé diviser
  - Faire : Devant un paquet ouvert, demander ce qu'il manque.
  - Attendu : Le nombre d'emplacements remplis, puis chaque objet manquant avec la quantité déjà déposée — par exemple « Blé, 2 sur 5 ».
- [ ] **paquets-paquet-emplacement** — touche : Flèches
  - Faire : Parcourir les emplacements du paquet aux flèches.
  - Attendu : Chaque emplacement annonce l'objet qu'il attend et où en est le dépôt, même s'il est vide — c'est justement vide qu'il porte l'information utile.
- [ ] **paquets-paquet-depot** — touche : Entrée
  - Faire : Déposer un objet, puis redemander ce qu'il manque.
  - Attendu : Le compte a bougé et correspond à ce qui vient d'être déposé.
- [ ] **paquets-paquet-complet** — touche : Pavé diviser
  - Faire : Sur un paquet entièrement rempli, demander l'état.
  - Attendu : « Paquet complet » plutôt qu'une liste vide.
- [ ] **paquets-paquet-coffre** — touche : Pavé diviser
  - Faire : Ouvrir un coffre ORDINAIRE et appuyer sur la même touche.
  - Attendu : « Aucun paquet ouvert » — un coffre normal ne doit pas être pris pour un paquet.

## 10. Pêche, mine et bûcheronnage

Les métiers qui font vivre la ferme. La pêche est la plus délicate : son mini-jeu est purement visuel.

- [ ] **metiers-peche-touche** — touche : —
  - Faire : Lancer la ligne et attendre.
  - Attendu : La touche du poisson est annoncée, et le résultat de la tentative aussi.
- [ ] **metiers-peche-bip** ⚠️ — touche : F8
  - Faire : Pêcher avec le bip de visée, puis le couper et recommencer.
  - Attendu : Le bip continu permet de suivre la zone de visée pendant le mini-jeu. C'est la fonctionnalité la plus expérimentale du mod.
- [ ] **metiers-minage** — touche : —
  - Faire : Frapper un rocher et un arbre, avec un outil trop faible puis adapté.
  - Attendu : Le mod signale l'outil trop faible, et confirme la casse sinon.

## 11. Combat et donjons

Se battre sans voir arriver les coups.

- [ ] **combat-combat-degats** ⚠️ — touche : —
  - Faire : Se faire toucher plusieurs fois.
  - Attendu : La santé restante est annoncée à chaque coup, avec une alerte de plus en plus pressante en bas.
- [ ] **combat-combat-etat** — touche : —
  - Faire : Entrer en combat, vaincre un ennemi, en sortir.
  - Attendu : Entrée et sortie de combat sont signalées, et l'ennemi vaincu annoncé.
- [ ] **combat-donjon** — touche : —
  - Faire : Entrer dans un donjon, nettoyer une salle.
  - Attendu : Le numéro d'étage est dit à l'entrée d'une salle, et la salle nettoyée confirmée.
- [ ] **combat-ennemis-scanner** — touche : Page haut/bas
  - Faire : Repérer les ennemis au scanner pendant un combat.
  - Attendu : Les ennemis proches sont listés avec direction et distance.

## 12. Bâtir et décorer

Tout nouveau, jamais testé. Si l'aperçu ne suit pas le curseur, c'est le pilotage de la souris qui est en cause.

- [ ] **batir-placement-mode** — touche : —
  - Faire : Prendre un meuble en main.
  - Attendu : « Mode placement » est annoncé, avec le nom de l'objet et son emprise si elle dépasse une case.
- [ ] **batir-placement-visee** ⚠️ — touche : Pavé décimal + flèches
  - Faire : Activer le curseur libre et déplacer la visée.
  - Attendu : L'aperçu du jeu suit la case du curseur.
- [ ] **batir-placement-validite** — touche : —
  - Faire : Balayer des cases valides puis invalides.
  - Attendu : « Emplacement valide » / « invalide » n'est dit qu'AUX BASCULES, sans couper la description de case.
- [ ] **batir-placement-pose** ⚠️ — touche : Pavé 5
  - Faire : Poser l'objet.
  - Attendu : L'objet se pose sur la case visée, pas devant le personnage.
- [ ] **batir-placement-etat** — touche : Pavé plus
  - Faire : Redemander l'état du placement.
  - Attendu : Objet, emprise et validité sont redits.

## 13. Progresser sur la durée

Les compétences, qu'on dépense au fil des niveaux.

- [ ] **progresser-competences** — touche : U puis Z
  - Faire : Demander les niveaux, puis les points disponibles.
  - Attendu : Niveaux par métier, puis points restants par arbre.
- [ ] **progresser-arbre-lecture** ⚠️ — touche : Flèches
  - Faire : Ouvrir l'arbre de compétences et se placer sur des nœuds.
  - Attendu : Chaque nœud dit son nom, son rang, s'il est verrouillé et à quelle condition, puis son effet.
- [ ] **progresser-arbre-deplacement** ⚠️ — touche : Flèches
  - Faire : Se déplacer de nœud en nœud dans la grille.
  - Attendu : Les flèches passent bien d'un nœud à l'autre — c'est ce qui n'a jamais été confirmé.
- [ ] **progresser-arbre-validation** — touche : Entrée
  - Faire : Prendre une compétence disponible.
  - Attendu : Le nœud est débloqué et le point décompté.

## 14. Réglages et confort

Ce qui s'ajuste plutôt que ce qui s'utilise. Ces réglages vivent dans le fichier de configuration du mod, et servent surtout de soupape : si un écran se comporte mal, ils permettent de rendre la main au jeu sans désinstaller quoi que ce soit.

- [ ] **reglages-tchat** — touche : C
  - Faire : Ouvrir le tchat ou la console du jeu, taper quelque chose, valider.
  - Attendu : C ouvre le tchat — pas Entrée, qui sert à valider dans les menus et entrait en conflit. La frappe est annoncée caractère par caractère, et aucune touche du mod ne se déclenche pendant.
- [ ] **reglages-mode-bref** — touche : —
  - Faire : Dans le fichier de configuration, section Navigation, passer ModeBref à false, relancer, et parcourir le sac.
  - Attendu : À true (défaut), le parcours n'annonce que la quantité et le nom. À false, la description complète suit chaque objet. Boutiques et artisanat ne changent pas dans les deux cas : prix et ingrédients y restent indispensables.
- [ ] **reglages-son-de-bord** — touche : —
  - Faire : Passer SonDeBord à false, relancer, et buter en bout de ligne dans le sac.
  - Attendu : Plus aucun bip aux extrémités, et la navigation reste sinon identique.
- [ ] **reglages-rendre-les-fleches** — touche : —
  - Faire : Passer NavigationDirectionnelle à false, relancer, et rouvrir un menu.
  - Attendu : Les flèches reviennent au jeu. C'est la soupape prévue si un écran se comporte mal — par exemple si le curseur saute deux cases d'un coup.
- [ ] **reglages-raccourcis-persistants** — touche : Suppr
  - Faire : Réassigner une touche dans le menu des raccourcis, quitter le jeu, relancer.
  - Attendu : La nouvelle touche est conservée, et l'aide F1 l'affiche à la place de l'ancienne.

