# Feuille de route — Sun Haven Access

Généré depuis `roadmap/roadmap.json` par `roadmap/build-roadmap.ps1`. **Ne pas éditer à la main.**

Mis à jour le 2026-08-27 — **7 sur 132 au point** (5 %).

| Marque | État | Sens | Nombre |
|---|---|---|---|
| `[ ]` | À tester | jamais essayé en jeu | 116 |
| `[!]` | À corriger | essayé, ne marche pas | 9 |
| `[~]` | À optimiser | marche, mais perfectible | 0 |
| `[x]` | Au point | essayé, rien à redire | 7 |

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
- [x] **avant-aide** — touche : F1
  - Faire : Appuyer sur F1, parcourir les rubriques aux flèches, puis fermer avec Échap ou F1.
  - Attendu : Un menu d'aide s'ouvre et annonce le nombre de rubriques. Les flèches passent d'une rubrique à la suivante, une seule est lue à la fois, et un bip signale les extrémités. Origine et Fin vont à la première et à la dernière. F1 est aussi une émote du jeu, le personnage fera donc un geste en plus.
  - Retour de jeu : Validé en jeu : rien à redire.
- [x] **avant-menu-raccourcis** — touche : Suppr
  - Faire : Ouvrir le menu des raccourcis, le parcourir, réassigner une touche.
  - Attendu : Chaque raccourci est annoncé avec son libellé et sa touche, et se change sans quitter le jeu.

## 2. Créer son personnage

Le tout premier écran d'une nouvelle partie. Il était totalement muet avant le mod : impossible de commencer à jouer.

- [ ] **personnage-menu-principal** — touche : Flèches, Entrée
  - Faire : Naviguer dans le menu principal et l'écran de sauvegarde.
  - Attendu : Les boutons sont nommés, et chaque sauvegarde annonce son résumé.
- [ ] **personnage-reglages-liste** ⚠️ — touche : Pavé 8
  - Faire : Ouvrir les réglages en liste, parcourir les options aux flèches haut et bas, en changer une avec gauche et droite. Essayer une case à cocher, un curseur et une liste déroulante.
  - Attendu : Chaque option est annoncée avec son nom ET sa valeur. Gauche et droite changent la valeur sur place, sans quitter la ligne. Les curseurs s'annoncent en pour cent. Les réglages du mod figurent en tête de liste.
  - Retour de jeu : Les réglages du mod — bip de bord, navigation directionnelle, mode bref — ne se changeaient jusqu'ici qu'en éditant le fichier de configuration à la main. Les contrôles du jeu sont pilotés directement, donc c'est lui qui applique et enregistre.
- [ ] **personnage-menu-principal-focus** ⚠️ — touche : —
  - Faire : Lancer le jeu et écouter sans rien presser, puis appuyer une fois sur la flèche du bas.
  - Attendu : Le premier bouton du menu — Jouer, ou Continuer si une partie existe — est annoncé d'emblée. La première flèche déplace ensuite d'UN cran, pas depuis le bas de la liste.
  - Retour de jeu : Rapporté en jeu : on arrivait tout en bas. Le jeu ne sélectionne rien de lui-même sur cet écran, il attend la souris ; le mod ne prenait donc la main qu'à la première flèche, qui déplaçait déjà la sélection depuis nulle part. Le bouton est choisi par sa POSITION — le plus haut — et non par son texte : « Jouer » devient « Continuer » quand une partie existe, et se traduit.
- [!] **personnage-menu-principal-regression** ⚠️ — touche : Flèches
  - Faire : Parcourir le menu principal du jeu aux flèches.
  - Attendu : Tous les boutons sont atteignables et nommés, au moins aussi bien qu'avant.
  - Retour de jeu : RÉGRESSION que j'ai introduite : le test « élément réellement à l'écran », ajouté pour écarter les panneaux des autres onglets, écartait aussi des boutons parfaitement visibles quand la conversion de coordonnées ne convenait pas à ce canevas. Corrigé — le test ne peut plus vider un écran : s'il ne laisse rien, on garde tout. Un écran inatteignable est bien pire que quelques éléments de trop.
- [ ] **personnage-sauvegardes-liste** ⚠️ — touche : Pavé 3
  - Faire : Sur l'écran de choix de personnage, ouvrir les sauvegardes en liste, choisir une partie, puis Charger. Réessayer sur un emplacement vide.
  - Attendu : Une ligne par partie : nom et date. Valider propose « Charger la partie de X » ou « Supprimer la partie de X », l'action nommée avec la partie. Un emplacement vide lance directement une nouvelle partie.
  - Retour de jeu : L'écran affiche chaque sauvegarde comme une fiche avec deux boutons quelque part dedans ; en suivant la mise en page, rien ne dit à quelle partie appartient le bouton où l'on se trouve. Les deux boutons déclenchés sont ceux du jeu, donc c'est lui qui charge, supprime, et impose sa confirmation.
- [ ] **personnage-sauvegardes-auto** ⚠️ — touche : —
  - Faire : Arriver sur l'écran de choix de personnage PAR LES DEUX CHEMINS : directement depuis le menu principal, puis par Jouer, Solo, Charger un personnage. Dans chaque cas, ne toucher à rien, puis fermer avec Échap et rouvrir avec Pavé 3.
  - Attendu : La liste des parties s'ouvre toute seule, sans raccourci à connaître, et contient TOUTES vos sauvegardes — y compris celles qui ne tiennent pas dans le cadre et qu'il faudrait faire défiler à la souris. Échap la referme et rend l'écran normal ; Pavé 3 la rouvre.
  - Retour de jeu : C'est le premier écran après le menu principal : devoir s'y souvenir d'un raccourci pour choisir sa partie, c'est buter dès la première minute. DÉFAUT SIGNALÉ ET CORRIGÉ : la liste ne s'ouvrait jamais seule. Je repérais l'écran en comptant les fiches trouvées dans toute la scène, alors qu'elles ne sont pas encore là — ou pas encore actives — au moment où l'écran s'ouvre. DEUX fausses pistes avant la bonne, notées pour qu'on n'y retourne pas. D'abord « y a-t-il des fiches dans la scène » : le menu principal en garde en permanence, hors champ, donc la réponse était vraie dès la première image et la transition « on vient d'arriver » ne se produisait jamais. Ensuite « l'objet loadCharacterMenu est-il actif » : exact pour un chemin, faux pour l'autre — le jeu mène à cet écran par deux routes, et restreindre la recherche à cet objet a du même coup cassé la touche Pavé 3. Ce qui vaut dans tous les cas est ce que le joueur voit : des fiches réellement À L'ÉCRAN. Le repli « si le test de présence ne laisse rien, on garde tout » est volontairement écarté pour la détection — il ramènerait le premier bug — mais conservé quand le joueur appuie sur la touche, puisqu'il nous dit alors où il est. TROISIÈME DÉFAUT, signalé depuis : avec plus de deux personnages, seuls deux apparaissaient. La liste défile, et je ne gardais que les fiches tenant dans le cadre — les autres, parfaitement réelles, étaient simplement plus bas. Une fiche visible sert désormais à identifier la LISTE, pas à en dresser le contenu : on remonte à son conteneur et on prend toutes ses sœurs, défilées ou non, dans l'ordre des frères plutôt que par position à l'écran, qui ne veut plus rien dire une fois défilée.
- [ ] **personnage-sauvegardes-details** — touche : Entrée
  - Faire : Valider une partie dans la liste, puis choisir « Détails complets ».
  - Attendu : Une liste donne la date, l'heure, l'emplacement, les pièces, orbes et tickets, les cinq niveaux de métier et la quête en cours — une entrée à la fois. La date est en FRANÇAIS, pas « Summer 11, Year 1 ».
  - Retour de jeu : « Supprimer » est passée en dernière position, derrière une action inoffensive : valider la première entrée d'une liste est un geste réflexe.
- [ ] **personnage-chargement-personnage** — touche : Flèches
  - Faire : Ouvrir le menu de chargement d'un personnage et parcourir les sauvegardes.
  - Attendu : Chaque sauvegarde est atteignable et annonce de quoi la reconnaître : nom et date. Un emplacement libre annonce qu'il est vide. La touche de description complète ajoute les niveaux et l'or.
  - Retour de jeu : Retravaillé. Un emplacement libre le dit désormais au lieu de rester muet — on passait devant sans savoir qu'on pouvait y commencer une partie. Et l'annonce est abrégée au nom et à la date, de quoi reconnaître la partie : les cinq niveaux de métier et le montant d'or à chaque déplacement rendaient le parcours interminable. Le détail reste lisible avec la touche de description complète.
- [ ] **personnage-creation-orientation** ⚠️ — touche : —
  - Faire : Arriver sur l'écran de création de personnage.
  - Attendu : Une annonce décrit d'emblée les trois colonnes, comment en changer, et prévient qu'un nom et une date d'anniversaire sont obligatoires.
- [ ] **personnage-creation-ce-qui-manque** ⚠️ — touche : —
  - Faire : Saisir un nom, puis définir l'anniversaire, en écoutant entre chaque étape.
  - Attendu : Le mod dit ce qui manque encore à chaque fois que ça change, puis « tout est prêt » quand le bouton Valider devient utilisable. Il ne le répète pas en boucle.
  - Retour de jeu : Sans ça, on appuie sur Valider et il ne se passe rien : le jeu grise le bouton et affiche ses avertissements en rouge à côté, ce qui n'apprend rien sans la vue.
- [ ] **personnage-race** — touche : Flèches
  - Faire : Choisir une race.
  - Attendu : Chaque race est annoncée au passage.
- [!] **personnage-colonnes** ⚠️ — touche : Ctrl + flèches
  - Faire : En création de personnage, sauter d'une colonne à l'autre : catégories à gauche, personnalisation au centre, informations à droite.
  - Attendu : « Colonne 2 sur 3 » est annoncé, suivi de l'élément atteint, d'une seule traite. On arrive à la même hauteur qu'on avait quittée. Un bip signale les bords.
  - Retour de jeu : Rapporté en jeu, deux fois. D'abord Ctrl+flèche ne faisait rien (le mod refusait les flèches tant que rien n'était sélectionné — blocage circulaire). Puis, au choix du métier, la colonne centrale se découpait en une colonne par icône, et le bandeau du bas en créait d'autres. Trois causes corrigées : le seuil de découpage est désormais relatif à la largeur de l'écran et non absolu ; l'écran est vu comme un empilement de bandes, Ctrl+haut/bas passant de l'une à l'autre ; et les éléments hors champ sont exclus. À réessayer.
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
  - Attendu : Santé, mana et bourse : pièces, plus orbes et tickets si vous en avez. Les monnaies à zéro sont passées sous silence, sauf les pièces.
- [ ] **premiers-pas-notifications** — touche : —
  - Faire : Provoquer un message du jeu (entrer dans une boutique fermée, action impossible).
  - Attendu : La bulle éphémère en haut à gauche est lue automatiquement.

## 4. Son sac et son équipement

La navigation dans les menus a été entièrement refaite et n'a jamais été confirmée en jeu. Le point à surveiller : qu'une pression ne provoque pas deux déplacements.

- [ ] **sac-onglets** — touche : Tab, puis Ctrl+Tab
  - Faire : Ouvrir le menu principal et passer d'un onglet à l'autre.
  - Attendu : Les 7 onglets sont nommés dans l'ordre : Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques, Paramètres.
- [ ] **sac-onglets-liste-sortie** ⚠️ — touche : Ctrl + haut, Ctrl + bas
  - Faire : Dans une liste d'onglet, faire Ctrl+haut. Puis changer d'onglet aux flèches plusieurs fois. Puis Ctrl+bas sur l'onglet voulu.
  - Attendu : Ctrl+haut ramène à la barre d'onglets. Ensuite, changer d'onglet ne rouvre PLUS la liste : on parcourt les onglets librement. Ctrl+bas entre dans la liste de l'onglet courant, et l'automatisme reprend.
  - Retour de jeu : Signalé en jeu : arriver sur l'arbre de compétences enfermait dans sa liste, puisqu'une liste ouverte capte tout le clavier — la barre d'onglets devenait inatteignable. Le geste réemployé est celui que le mod utilise déjà partout pour changer de zone, plutôt qu'une touche de plus à retenir. Le drapeau « rester sur les onglets » était indispensable : sans lui, la flèche suivant Ctrl+haut changeait d'onglet et rouvrait la liste aussitôt, ce qui annulait la sortie.
- [ ] **sac-onglets-liste-auto** ⚠️ — touche : Tab, puis gauche et droite
  - Faire : Ouvrir le menu avec Tab, puis passer sur Compétences, Relations, Quêtes, Carte, Statistiques et Paramètres, sans appuyer sur aucune autre touche.
  - Attendu : Chaque onglet ouvre SA liste tout seul, dès qu'on arrive dessus. Haut et bas la parcourent une entrée à la fois, Entrée valide, Échap referme. Le sac à dos ne change pas : il garde sa grille.
  - Retour de jeu : Les listes existaient déjà, mais derrière des touches séparées — G, V, Z, X, Pavé 8. Une fonctionnalité qu'il faut deviner n'existe pas vraiment. Ces touches restent utilisables pour consulter les mêmes listes SANS ouvrir le menu.
- [ ] **sac-onglets-liste-silence** ⚠️ — touche : Tab puis flèches
  - Faire : Une liste d'onglet ouverte, parcourir aux flèches et écouter attentivement.
  - Attendu : SEULE la liste parle. Le panneau du jeu, dessous, reste muet — pas de double annonce, pas de nom de bouton qui se superpose.
  - Retour de jeu : C'est le point le plus fragile de tout l'aiguillage : le jeu lit les touches par Rewired, donc sa sélection continue de bouger sous la liste. Les lecteurs concurrents sont mis en sourdine quand une liste est ouverte, mais ce correctif n'a jamais été vérifié en jeu.
- [ ] **sac-statistiques-liste** — touche : Tab jusqu'à Statistiques
  - Faire : Ouvrir l'onglet Statistiques et parcourir la liste.
  - Attendu : Chaque entrée réunit un intitulé et sa valeur — « Récoltes : 142 » — et non deux entrées séparées dont la seconde ne voudrait rien dire.
  - Retour de jeu : C'est le seul onglet sans aucune donnée derrière lui : le panneau ne contient que du texte mis en page. Les lignes sont donc reconstituées en groupant les textes par hauteur. Si des valeurs se retrouvent mal appariées, c'est ici.
- [!] **sac-retour-onglets** ⚠️ — touche : Ctrl + haut
  - Faire : Ouvrir l'onglet Compétences (ou n'importe quel autre), descendre dans le contenu, puis remonter à la barre d'onglets.
  - Attendu : Ctrl+haut ramène TOUJOURS à la barre d'onglets, depuis n'importe quel panneau et quelle que soit la profondeur.
  - Retour de jeu : Signalé puis retiré : après plusieurs Tab la navigation redevenait bonne. Le défaut existait néanmoins dans le code — au sommet de l'écran, le saut de bandes bipait et consommait la touche, si bien que la règle « Ctrl+haut ramène aux onglets » n'était jamais atteinte. Corrigé. Le tâtonnement venait d'ailleurs : la barre d'onglets ne figurait pas dans l'ordre d'entrée, donc la première flèche après Tab atterrissait dans le sac à dos. Elle y est désormais en premier.
- [!] **sac-onglets-message-parasite** ⚠️ — touche : Tab puis flèches
  - Faire : Ouvrir Relations, Carte, Quêtes, Statistiques et Paramètres, puis parcourir chacun aux flèches.
  - Attendu : Chaque onglet lit SON contenu. Plus aucun message d'arbre de compétences dans un autre onglet.
  - Retour de jeu : RÉGRESSION que j'ai introduite : le repli qui rend parcourables les onglets sans élément cliquable ramassait TOUS les textes actifs de la scène, y compris ceux des panneaux voisins que le jeu garde hors champ. D'où le message d'arbre de compétences partout. Le filtre de présence à l'écran lui manquait ; il l'a maintenant, avec le même garde-fou qu'ailleurs — s'il ne laisse rien, on garde tout.
- [ ] **sac-bandes-nommees** — touche : Flèches
  - Faire : Dans l'arbre de compétences, descendre d'une rangée à l'autre à la flèche du bas, et se déplacer latéralement à la flèche droite.
  - Attendu : Les FLÈCHES SEULES suffisent pour tout parcourir. Chaque changement de rangée annonce son intitulé — « Mobilité », « Bûcheronnage », « Collecte », « Social ». Aucun Contrôle n'est nécessaire à l'intérieur de la grille.
  - Retour de jeu : Rapporté en jeu : « trop de Ctrl+flèches ». Cause trouvée — le regroupement en colonnes prenait la médiane de TOUS les écarts, or dans une grille la moitié valent zéro, donc la médiane s'effondrait et le seuil découpait la grille colonne de nœud par colonne de nœud. Chaque déplacement latéral exigeait alors un Contrôle. La référence est maintenant la médiane des écarts NON NULS, c'est-à-dire l'espacement réel. Et l'intitulé de rangée est passé de Ctrl+haut/bas à la flèche simple : descendre d'une rangée est un déplacement ordinaire.
- [ ] **sac-onglets-tous** ⚠️ — touche : Tab puis flèches
  - Faire : Ouvrir chacun des sept onglets et tenter de parcourir son contenu : Sac à dos, Compétences, Relations, Quêtes, Carte, Statistiques, Paramètres.
  - Attendu : Chaque onglet a quelque chose de parcourable et de lu. Les onglets sans rien de cliquable — les Statistiques notamment — deviennent parcourables ligne de texte par ligne de texte, à défaut de mieux.
  - Retour de jeu : Signalé en jeu : Relations, Carte, Statistiques et Paramètres n'étaient pas accessibles. Quêtes l'était déjà par la touche G, Relations par V et la Carte par X et Y, mais ce sont des annonces à la demande, pas une navigation. Un repli sur les textes est ajouté pour les panneaux sans élément cliquable. Dites-moi lesquels restent muets : s'il en reste, c'est qu'ils n'exposent ni bouton ni texte standard, et il faudra un navigateur dédié comme pour la carte du monde.
- [ ] **sac-nav-directionnelle** ⚠️ — touche : Flèches
  - Faire : Dans le sac, se déplacer ligne par ligne et colonne par colonne.
  - Attendu : Les flèches suivent la disposition visuelle réelle. Gauche/droite butent en bout de ligne avec un bip. Une pression = un seul déplacement.
- [!] **sac-onglet-ctrl-bas** ⚠️ — touche : Ctrl + bas
  - Faire : Ouvrir chaque onglet du menu principal (Sac, Compétences, Relations, Quêtes, Carte, Statistiques) et faire Ctrl+bas depuis la barre d'onglets.
  - Attendu : On descend dans le contenu de l'onglet RÉELLEMENT ouvert. Un deuxième Ctrl+bas descend encore d'une bande — par exemple vers la barre des métiers, puis vers la grille de compétences.
  - Retour de jeu : Rapporté en jeu : quel que soit l'onglet, Ctrl+bas atterrissait dans l'arbre de compétences. Cause trouvée — le mod ne testait que la transparence, jamais si l'élément était réellement à l'écran, et les panneaux des autres onglets restent actifs hors champ. Ils étaient donc tous candidats. Corrigé par un test de présence à l'écran.
- [!] **sac-competences-bandes** ⚠️ — touche : Ctrl + bas puis flèches
  - Faire : Dans l'arbre de compétences : Ctrl+bas jusqu'à la barre des métiers (Exploration, Agriculture, Miner, Combat, Pêcher), puis encore Ctrl+bas pour entrer dans la grille des nœuds.
  - Attendu : Chaque bande est atteignable dans l'ordre, et les flèches seules parcourent la bande courante avec un mur sonore aux quatre bords.
- [!] **sac-relations-menu** — touche : Flèches
  - Faire : Ouvrir l'onglet Relations et tenter de parcourir la liste des personnages.
  - Attendu : Chaque personnage est atteignable et annoncé avec ses cœurs.
  - Retour de jeu : Rapporté en jeu : le menu n'est pas parcourable. Le test de présence à l'écran peut suffire à le débloquer si le problème venait des panneaux voisins ; sinon les entrées de relations n'exposent probablement aucun élément sélectionnable, et il faudra un navigateur dédié comme pour la carte du monde. À revérifier pour trancher.
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

- [ ] **ferme-mana-alertes** ⚠️ — touche : —
  - Faire : Miner ou labourer jusqu'à épuiser le mana.
  - Attendu : Une alerte à la moitié, au quart, presque à sec, puis à l'épuisement. Chacune n'est dite qu'une fois, et n'interrompt pas l'annonce en cours.
- [ ] **ferme-mana-pas-de-spam** — touche : —
  - Faire : Sous un seuil, consommer et regagner un peu de mana plusieurs fois de suite.
  - Attendu : L'alerte ne se répète pas à chaque oscillation. Elle ne revient qu'après une vraie remontée au-dessus du seuil.
- [ ] **ferme-mana-lendemain** — touche : —
  - Faire : Épuiser le mana, dormir, puis retravailler le lendemain.
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

- [ ] **village-monture** — touche : —
  - Faire : Utiliser le sifflet pour monter, puis à nouveau pour descendre.
  - Attendu : « En selle » puis « à pied » sont annoncés à chaque changement, sans couper l'annonce en cours.
- [ ] **village-monture-interieur** — touche : —
  - Faire : Essayer de monter à l'intérieur d'un bâtiment.
  - Attendu : Le refus du jeu est lu (c'est une notification), et aucun « en selle » n'est annoncé à tort.
- [ ] **village-monture-carte** — touche : —
  - Faire : Monter, franchir un portail vers une autre carte, puis descendre.
  - Attendu : Les annonces continuent après le changement de carte — c'est là que le suivi pourrait se perdre.
- [ ] **village-npc-proche** — touche : N
  - Faire : Appuyer sur N plusieurs fois près de personnages.
  - Attendu : On passe d'un personnage proche au suivant, avec nom, direction et distance.
- [!] **village-dialogue-choix** ⚠️ — touche : —
  - Faire : Parler à un personnage jusqu'à obtenir une question à choix, puis sélectionner une réponse aux flèches.
  - Attendu : La question est suivie de « 2 choix : 1, Oui ; 2, Non. » d'emblée. En naviguant entre les réponses, le mod ne doit PAS relire la question à chaque déplacement.
  - Retour de jeu : Rapporté en jeu : les flèches étaient pénibles pour choisir une réponse et un message se répétait. Cause trouvée — les options d'une bulle ne sont pas des éléments sélectionnables mais de simples textes pilotés par le jeu ; le mod s'emparait quand même des flèches, ne trouvait rien, rejouait son annonce de repli et empêchait le jeu de changer d'option. Le mod rend désormais les flèches au jeu pendant tout dialogue ou cinématique, et les réponses sont énoncées d'emblée avec la question. À réessayer.
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
- [ ] **village-menus-un-seul** — touche : F1, Suppr
  - Faire : Ouvrir une liste (quêtes, relations, compétences), puis appuyer sur F1 ou Suppr par-dessus.
  - Attendu : La liste se ferme et l'autre menu s'ouvre. En le refermant on revient au jeu, pas dans la liste précédente.
  - Retour de jeu : Deux menus vocaux se croyaient chacun maître des flèches. Un seul à la fois désormais. Les sous-menus — un métier dans les compétences, une action sur une sauvegarde — ne disent plus « fermé » juste avant d'ouvrir la suite.
- [!] **village-listes-exclusives** ⚠️ — touche : G, V, Z
  - Faire : Ouvrir le menu Tab, puis ouvrir une liste par-dessus (quêtes, relations, compétences) et la parcourir aux flèches.
  - Attendu : SEULE la liste parle. Le menu du jeu dessous ne doit rien annoncer, même s'il continue de bouger.
  - Retour de jeu : Rapporté en jeu : « le système de liste n'a rien changé ». Cause probable trouvée — rendre les flèches inertes ne suffit pas, Sun Haven lit le clavier par Rewired, indépendamment du système d'évènements d'Unity, donc sa sélection continuait de bouger sous la liste. Les deux étaient annoncés en même temps, chacun coupant l'autre. Les trois lecteurs du mod (focus, infobulle, survol) se taisent maintenant tant qu'une liste est ouverte. DEUXIÈME DÉFAUT, signalé depuis : le curseur de case, lui, continuait de décrire le terrain — chaque flèche dans la liste des relations était suivie d'une description mise en file derrière l'entrée qu'on voulait entendre. La règle était recopiée à trois endroits et le quatrième avait été oublié ; elle tient désormais en un seul (Menus/VoiceMenus.cs), ce qui couvre aussi l'assistant de placement.
- [ ] **village-carte-liste** — touche : X
  - Faire : Ouvrir la carte du monde, puis la liste des lieux. Parcourir, valider un lieu.
  - Attendu : Tous les lieux de la région en une liste, avec leur nombre. Valider ouvre le lieu et annonce sa description.
  - Retour de jeu : Le cheminement à pied depuis la carte a été demandé mais N'EST PAS possible : un lieu de la carte est une icône d'interface, sa position à l'écran n'a aucun rapport avec un endroit du monde, et s'en servir enverrait marcher vers une coordonnée arbitraire. Le jeu ne rattache à ces icônes qu'un nom de scène. Pour se déplacer, c'est le scanner, qui vise de vrais objets avec leur position réelle.
- [ ] **village-listes-simplifiees** ⚠️ — touche : G, V, Z
  - Faire : Ouvrir les quêtes (G), les relations (V) et les compétences (Z). Parcourir chaque liste aux flèches, sauter au début et à la fin, fermer avec Échap. Dans les compétences, choisir un métier avec Entrée.
  - Attendu : Chaque panneau devient une LISTE : une entrée à la fois, annoncée avec sa position, un bip aux extrémités. Rien à se représenter de la disposition à l'écran. Les compétences se lisent en deux temps : les métiers, puis les compétences du métier choisi.
  - Retour de jeu : Changement de principe demandé en jeu : ne plus suivre l'affichage. Ces écrans sont conçus pour l'œil — la position y porte le sens — alors que leur contenu est une simple liste. Le mod lit désormais les DONNÉES du jeu et les présente comme telles, sur le modèle de l'aide F1, seul écran validé sans réserve. Le sac à dos garde sa navigation en grille, qui convient à une grille.
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

- [ ] **combat-sorts-equiper** ⚠️ — touche : Pavé 7
  - Faire : Appuyer sur Pavé 7 pour ouvrir le choix de sort du premier emplacement, parcourir les sorts aux flèches, en équiper un avec Entrée. Réappuyer pour passer aux emplacements suivants, puis fermer.
  - Attendu : Chaque sort disponible est annoncé par son nom, et Entrée l'équipe réellement dans l'emplacement. La liste correspond à ce que vous avez débloqué.
  - Retour de jeu : Changer un sort équipé était IMPOSSIBLE sans souris : le menu s'ouvre au survol et n'expose rien de sélectionnable. Le mod ouvre maintenant le menu du JEU et y pose la sélection clavier, plutôt que de reconstruire une liste — ce qui aurait obligé à recopier une douzaine de conditions de déblocage dispersées dans les arbres de compétences.
- [ ] **combat-sorts-equipes** — touche : Pavé 9
  - Faire : Demander les sorts équipés, avec des emplacements remplis et d'autres vides.
  - Attendu : Les quatre emplacements sont annoncés dans l'ordre, avec le nom du sort ou « vide ». Les noms doivent être ceux du jeu, pas des identifiants techniques.
- [ ] **combat-sorts-recharge** — touche : —
  - Faire : Lancer un sort deux fois de suite, puis en lancer un sans avoir assez de mana.
  - Attendu : Le jeu annonce lui-même « en recharge » et « pas assez de mana » — le mod ne fait que les lire. Vérifier que ces deux messages passent bien.
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

## 14. Jouer en anglais

Le mod parle désormais anglais, en suivant la langue du jeu. Le français reste la seule langue vérifiée à l'oreille : l'anglais n'a encore été essayé par personne, et c'est tout l'objet de cette étape.

- [ ] **langue-langue-auto** ⚠️ — touche : —
  - Faire : Passer Sun Haven en anglais dans ses options, puis relancer et écouter les annonces du mod.
  - Attendu : Le mod parle anglais sans qu'on ait rien réglé. Repasser le jeu en français le fait reparler français.
  - Retour de jeu : La langue est lue sur le gestionnaire de traduction du jeu, par réflexion plutôt que par une référence à sa bibliothèque : si une mise à jour la renommait, le mod retomberait sur le français au lieu de refuser de se charger.
- [ ] **langue-langue-reglage** — touche : Pavé 8
  - Faire : Ouvrir les réglages : la langue des annonces est la première entrée. La faire défiler aux flèches gauche et droite.
  - Attendu : Trois valeurs — comme le jeu, Français, English — et le changement prend effet aussitôt.
  - Retour de jeu : C'est le seul réglage dont on peut avoir besoin sans comprendre un mot de ce que le mod dit : le laisser dans le fichier de configuration reviendrait à demander d'éditer un fichier à l'aveugle, dans une langue qu'on ne lit pas.
- [ ] **langue-langue-aide** ⚠️ — touche : F1
  - Faire : En anglais, ouvrir l'aide et parcourir toutes les rubriques.
  - Attendu : Toutes les rubriques sont en anglais, aucune ne manque. Les noms de touches aussi.
  - Retour de jeu : L'aide est écrite deux fois, en regard : c'est la seule façon que chaque version se lise naturellement. Un garde-fou journalise l'écart si une rubrique n'est ajoutée que d'un côté — une aide qu'on n'entend pas ne se remarque jamais en jeu.
- [ ] **langue-langue-raccourcis** — touche : Suppr
  - Faire : En anglais, ouvrir le menu des raccourcis et parcourir toutes les actions.
  - Attendu : Le nom ET l'explication de chaque touche sont en anglais.
  - Retour de jeu : Les 47 actions et leurs descriptions restent écrites en français dans le code — elles servent aussi de commentaires dans le fichier de configuration. Vérifié qu'aucune n'a de trou dans la table de traduction.
- [ ] **langue-langue-melange** ⚠️ — touche : —
  - Faire : En anglais, jouer normalement une bonne session et guetter tout mot français au milieu d'une phrase anglaise.
  - Attendu : Aucun mélange. Une phrase non traduite doit ressortir ENTIÈREMENT en français, pas à moitié : c'est la règle posée dans le code, et c'est ce qu'il faut vérifier.
  - Retour de jeu : Une phrase à moitié traduite est pire que la version française — elle donne à un anglophone un mot sur deux, et à un francophone une phrase cassée. Signalez toute phrase mélangée : c'est le défaut le plus probable ici.
- [ ] **langue-langue-noms-du-jeu** — touche : —
  - Faire : En anglais, examiner des objets, des personnages, des lieux et des terrains.
  - Attendu : Leurs noms sont en anglais, tels que le jeu les donne — pas traduits en français par le mod.
  - Retour de jeu : Le mod porte un dictionnaire qui met en français les noms techniques anglais quand le jeu n'affiche aucun texte. En anglais ce dictionnaire s'efface : la source est déjà dans la bonne langue, il ne reste qu'à découper les mots.

## 15. Jouer à plusieurs

Le mod est entièrement local : il n'envoie ni ne reçoit rien sur le réseau, et vos partenaires n'ont rien à installer pour jouer avec vous. Reste à vérifier qu'il ne vous annonce que VOS actions — un partenaire qui pêche à côté ne doit pas déclencher vos annonces. Ces points demandent une seconde personne, et n'ont donc jamais pu être essayés.

- [ ] **cooperation-coop-sans-mod** ⚠️ — touche : —
  - Faire : Jouer avec quelqu'un qui n'a PAS installé le mod, une bonne session.
  - Attendu : La partie se déroule normalement pour tous les deux. Votre partenaire ne constate aucune différence, aucun blocage, aucun décalage.
  - Retour de jeu : Vérifié dans le code : sur 26 correctifs appliqués au jeu, un seul annule un appel, et uniquement pour l'objet du mod lui-même, comparé par référence. Tout le reste laisse le jeu faire. Reste à le confirmer en conditions réelles.
- [ ] **cooperation-coop-arrivee-depart** — touche : —
  - Faire : Faire rejoindre puis quitter la partie à quelqu'un. Puis lui faire traverser plusieurs bâtiments.
  - Attendu : « Untel a rejoint la partie » et « Untel a quitté la partie ». Traverser une porte ne déclenche RIEN : changer de carte détruit et recrée le joueur, et un départ n'est annoncé qu'après plusieurs secondes d'absence.
- [ ] **cooperation-coop-silence** ⚠️ — touche : —
  - Faire : Rester à côté de votre partenaire pendant qu'il prend des coups, pêche, coupe un arbre et tue des ennemis. Ne rien faire vous-même.
  - Attendu : AUCUNE annonce. Ni « Touché », ni « Ça mord », ni bip de visée, ni ennemi vaincu.
  - Retour de jeu : Défaut réel trouvé et corrigé : les annonces de dégâts, de mort et les cinq annonces de pêche ne vérifiaient pas de QUI il s'agissait. En coopération, les coups reçus par votre partenaire étaient annoncés comme les vôtres, avec sa santé.
- [ ] **cooperation-coop-mes-actions** ⚠️ — touche : —
  - Faire : Faire vous-même les mêmes gestes : prendre un coup, pêcher, récolter, tuer un ennemi.
  - Attendu : Tout est annoncé comme d'habitude. Le filtre ne doit pas vous rendre muet à vous aussi.
- [ ] **cooperation-coop-qui-parle** — touche : —
  - Faire : Faire écrire votre partenaire dans le tchat, et faire parler un personnage du jeu.
  - Attendu : Chaque message est précédé du nom de celui qui parle, pour qu'un partenaire ne se confonde pas avec un personnage du jeu.
- [ ] **cooperation-coop-scanner** — touche : Ctrl + Page, puis Page
  - Faire : Ouvrir la catégorie Personnages du scanner pendant qu'un partenaire est à proximité.
  - Attendu : Il y figure, annoncé « son nom, joueur », distinct des personnages du jeu, avec sa direction et sa distance. Se rendre jusqu'à lui fonctionne.
  - Retour de jeu : Les joueurs ne vivent pas dans la scène de la carte mais dans une scène persistante : le filtre de scène du scanner les écartait tous. Corrigé.

## 16. Réglages et confort

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
- [ ] **reglages-raccourci-non-assigne** — touche : Suppr
  - Faire : Dans le menu des raccourcis, choisir une action, puis appuyer sur Retour arrière ou Suppression au lieu d'une touche. Vérifier ensuite qu'elle ne se déclenche plus, et la réassigner.
  - Attendu : L'action annonce « non assigné » et ne se déclenche plus, tout en restant dans la liste avec sa description. La réassigner la rétablit.
  - Retour de jeu : F10 (décrire la case devant soi) et Fin (nombre trouvé au scanner) sont désormais livrées SANS touche, sur votre retour : la case devant soi est déjà décrite à chaque pas, et le nombre trouvé est déjà annoncé au changement de catégorie. Les deux restent dans le menu des raccourcis, réassignables quand vous voulez.
- [ ] **reglages-raccourcis-persistants** — touche : Suppr
  - Faire : Réassigner une touche dans le menu des raccourcis, quitter le jeu, relancer.
  - Attendu : La nouvelle touche est conservée, et l'aide F1 l'affiche à la place de l'ancienne.

