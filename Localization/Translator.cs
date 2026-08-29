using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SunHavenAccess.Localization
{
    /// <summary>
    /// Traduit les annonces du mod en anglais, au moment de les prononcer.
    ///
    /// Le français reste la seule langue écrite dans le code : c'est la langue dans laquelle le mod
    /// a été pensé et testé, et la seule dont chaque tournure a été vérifiée à l'oreille par un
    /// utilisateur de lecteur d'écran. La traduction est donc une couche par-dessus, et non un
    /// remplacement des cent soixante-quinze appels répartis dans cinquante-six fichiers : les
    /// modifier tous ferait courir à chaque annonce le risque d'une régression, pour une langue
    /// que personne n'a encore essayée.
    ///
    /// Deux étages, dans cet ordre :
    ///
    /// 1. Les phrases entières, traduites exactement. C'est le gros des annonces.
    /// 2. Les phrases composées à l'exécution — « Santé : 45 sur 100 » — reconnues par motif, les
    ///    valeurs étant reportées telles quelles.
    ///
    /// Règle absolue : ce qui n'est pas reconnu ressort EN FRANÇAIS, intact. Une phrase à moitié
    /// traduite est pire que la version française — elle donne à un anglophone un mot sur deux, et
    /// à un francophone une phrase cassée. Mieux vaut une lacune franche qu'un mélange.
    /// </summary>
    public static class Translator
    {
        public static string Translate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            if (!Language.IsEnglish) return text;

            if (Sentences.TryGetValue(text.Trim(), out string exact)) return exact;

            foreach (var pattern in Patterns)
            {
                Match m = pattern.Key.Match(text);
                if (!m.Success) continue;

                return pattern.Key.Replace(text, match => Substitute(match, pattern.Value));
            }

            return text; // non reconnu : le français intact, jamais un mélange
        }

        /// <summary>
        /// Construit le remplacement, en repassant chaque groupe capturé par la table des phrases.
        ///
        /// Sans cela, « Statistiques : rien à afficher. » deviendrait « Statistiques: nothing to
        /// show. » — le motif traduit, le titre resté en français. C'est précisément le mélange
        /// qu'on cherche à éviter. Les groupes qui ne sont pas dans la table ressortent intacts :
        /// ce sont alors des noms d'objets, de lieux ou de personnages, que le jeu a déjà donnés
        /// dans SA langue, et qu'il ne faut surtout pas toucher.
        ///
        /// Seule la table des phrases est consultée, jamais les motifs : la récursion s'arrête donc
        /// nécessairement.
        /// </summary>
        private static string Substitute(Match match, string template)
        {
            return GroupReference.Replace(template, reference =>
            {
                int index = int.Parse(reference.Groups[1].Value);
                if (index >= match.Groups.Count) return reference.Value;

                string captured = match.Groups[index].Value;
                return Sentences.TryGetValue(captured.Trim(), out string translated)
                    ? translated
                    : captured;
            });
        }

        private static readonly Regex GroupReference = new Regex(@"\$(\d+)");

        /// <summary>Phrases fixes, telles qu'écrites dans le code.</summary>
        private static readonly Dictionary<string, string> Sentences = new Dictionary<string, string>
        {
            // État et actions
            { "Activé.", "On." },
            { "Coché.", "Ticked." },
            { "Décoché.", "Unticked." },
            { "Arbre abattu.", "Tree felled." },
            { "Arrosoir rempli.", "Watering can filled." },
            { "Arrosoir vide.", "Watering can empty." },
            { "Arrosé.", "Watered." },
            { "Planté.", "Planted." },
            { "Récolté.", "Harvested." },
            { "Rocher brisé.", "Rock broken." },
            { "Terre labourée.", "Soil tilled." },
            { "Culture infusée de mana.", "Crop infused with mana." },
            { "Hache trop faible pour cet arbre.", "Axe too weak for this tree." },
            { "Pioche trop faible pour ce rocher.", "Pickaxe too weak for this rock." },
            { "Sac à dos trié.", "Backpack sorted." },
            { "Sac à dos vide.", "Backpack empty." },
            { "Rien à ranger dans les coffres proches.", "Nothing to put away in nearby chests." },
            { "Aucun coffre à proximité.", "No chest nearby." },

            // Combat
            { "En combat.", "In combat." },
            { "Combat terminé.", "Combat over." },
            { "Vous êtes tombé au combat.", "You have fallen in combat." },
            { "Salle nettoyée, porte ouverte.", "Room cleared, door open." },

            // Pêche
            { "Ça mord !", "It's biting!" },
            { "Le poisson s'est échappé.", "The fish got away." },

            // Déplacement et cheminement
            { "Cheminement annulé.", "Pathing cancelled." },
            { "Arrêté, obstacle sur la suite du chemin.", "Stopped, obstacle further along the path." },
            { "Erreur pendant le calcul du chemin.", "Error while working out the path." },
            { "Curseur libre désactivé.", "Free cursor off." },
            { "Sélectionnez d'abord un élément avec le scanner.", "Pick something with the scanner first." },
            { "Cet élément n'est plus disponible, nouvelle recherche.", "That is no longer available, searching again." },
            { "Cet élément n'existe plus, nouvelle recherche.", "That no longer exists, searching again." },

            // Menus et listes
            { "Liste vide.", "Empty list." },
            { "Aide vide.", "Help is empty." },
            { "Aide fermée.", "Help closed." },
            { "Menu des raccourcis fermé.", "Shortcuts menu closed." },
            { "Changement annulé.", "Change cancelled." },
            { "Clic droit.", "Right click." },
            { "Rien à activer.", "Nothing to activate." },
            { "Aucun élément sélectionné.", "Nothing selected." },
            { "Aucun élément de menu détecté à l'écran.", "No menu element detected on screen." },
            { "Le menu n'est pas ouvert.", "The menu is not open." },
            { "Cette action n'est pas disponible.", "That action is not available." },
            { "Ce réglage n'a pas pu être modifié.", "That setting could not be changed." },
            { "Utilisez les flèches pour sélectionner un élément avant de valider.",
              "Use the arrows to pick something before confirming." },

            // Écrans particuliers
            { "La carte n'est pas ouverte.", "The map is not open." },
            { "Aucun lieu trouvé sur cette carte.", "No location found on this map." },
            { "La création de personnage n'est pas ouverte.", "Character creation is not open." },
            { "Le jeu n'est pas encore chargé.", "The game has not loaded yet." },
            { "Aucune sauvegarde à afficher ici.", "No save to show here." },
            { "Aucune statistique à afficher.", "No statistics to show." },

            // Sorts
            { "Aucun sort équipé.", "No spell equipped." },
            { "Sorts illisibles pour le moment.", "Spells unreadable at the moment." },
            { "Sorts : ", "Spells: " },
            { "Choix des sorts fermé.", "Spell picker closed." },
            { "Impossible d'ouvrir le choix des sorts.", "Cannot open the spell picker." },
            { "Le choix des sorts n'est pas disponible ici.", "The spell picker is not available here." },

            // Compétences, quêtes, relations
            { "Aucun métier trouvé.", "No profession found." },
            { "Aucune donnée de compétence disponible.", "No skill data available." },
            { "Les compétences ne sont pas disponibles ici. Ouvrez l'arbre de compétences.",
              "Skills are not available here. Open the skill tree." },
            { "Aucune quête active.", "No active quest." },
            { "Aucune relation nouée pour l'instant.", "No relationships yet." },
            { "Aucun personnage à proximité.", "No character nearby." },

            // Ferme, paquets, panneaux
            { "Aucun animal ici.", "No animal here." },
            { "Aucun paquet ouvert.", "No bundle open." },
            { "Aucun panneau d'affichage à proximité.", "No bulletin board nearby." },
            { "Panneau d'affichage trouvé, mais son type est illisible.",
              "Bulletin board found, but its type is unreadable." },

            // Placement
            { "Aucun objet à poser en main.", "Nothing in hand to place." },
            { "Mode placement quitté.", "Left placement mode." },

            // Titres de listes et catégories du scanner. Ils sont rarement prononcés seuls : ils
            // arrivent surtout comme groupe capturé d'un motif — « Statistiques : rien à
            // afficher. » — et c'est Substitute qui les repasse par cette table.
            { "Sac à dos", "Backpack" },
            { "Arbre de compétences", "Skill tree" },
            { "Quêtes", "Quests" },
            { "Carte", "Map" },
            { "Paramètres", "Settings" },
            { "Lieux de la carte", "Map locations" },
            { "Métiers", "Professions" },
            { "Quêtes actives", "Active quests" },
            { "Relations", "Relationships" },
            { "Réglages", "Settings" },
            { "Sauvegardes", "Saves" },
            { "Statistiques", "Statistics" },
            { "Personnages", "Characters" },
            { "Plantations", "Crops" },
            { "Ressources", "Resources" },
            { "Entrées de bâtiment", "Building entrances" },
            { "Changements de zone", "Area exits" },
            { "Animaux et compagnons", "Animals and pets" },
            { "Ennemis", "Enemies" },
            { "Mobilier et rangement", "Furniture and storage" },
            { "Services et repères", "Services and landmarks" },

            // Les libellés d'actions du menu des raccourcis. Ils sont lus tels quels dans la liste,
            // et arrivent aussi comme groupe capturé de « Touche pour X changée en Y » — que
            // Substitute repasse par cette table. Une seule liste sert donc aux deux usages.
            { "Aide", "Help" },
            { "Décrire la case devant vous", "Describe the tile in front of you" },
            { "Annoncer votre position", "Announce your position" },
            { "Personnage proche suivant", "Next nearby character" },
            { "Répéter la dernière annonce", "Repeat the last announcement" },
            { "Verbosité des déplacements", "Movement verbosity" },
            { "Élément de menu précédent", "Previous menu item" },
            { "Élément de menu suivant", "Next menu item" },
            { "Activer l'élément de menu (clic gauche)", "Activate the menu item (left click)" },
            { "Tourner à gauche sans se déplacer", "Turn left without moving" },
            { "Tourner à droite sans se déplacer", "Turn right without moving" },
            { "Test sonore (diagnostic)", "Sound test (diagnostic)" },
            { "Activer/désactiver la souris directionnelle", "Toggle the directional mouse" },
            { "Simuler un clic gauche (monde)", "Simulate a left click (world)" },
            { "Simuler un clic droit (monde, avec Ctrl)", "Simulate a right click (world, with Ctrl)" },
            { "Ouvrir le menu des raccourcis", "Open the shortcuts menu" },
            { "Scanner : élément précédent (Ctrl = catégorie précédente)",
              "Scanner: previous item (Ctrl = previous category)" },
            { "Scanner : élément suivant (Ctrl = catégorie suivante)",
              "Scanner: next item (Ctrl = next category)" },
            { "Scanner : annoncer l'élément sélectionné (Ctrl = s'y rendre)",
              "Scanner: announce the selected item (Ctrl = travel to it)" },
            { "Scanner : annoncer le nombre trouvé", "Scanner: announce how many were found" },
            { "Ouvrir le tchat / la console du jeu", "Open the game's chat or console" },
            { "Annoncer l'heure, le jour, la saison et la météo",
              "Announce the time, day, season and weather" },
            { "Annoncer la santé et le mana", "Announce health and mana" },
            { "Activer/désactiver le bip de visée en pêche", "Toggle the fishing aim beep" },
            { "Annoncer les quêtes actives", "Announce active quests" },
            { "Annoncer les relations", "Announce relationships" },
            { "Annoncer les niveaux de compétence", "Announce skill levels" },
            { "Carte : liste des lieux", "Map: list of locations" },
            { "Carte : lieu suivant", "Map: next location" },
            { "Apparence : précédent (Ctrl = catégorie)", "Appearance: previous (Ctrl = category)" },
            { "Apparence : suivant (Ctrl = catégorie)", "Appearance: next (Ctrl = category)" },
            { "Compétences (liste)", "Skills (list)" },
            { "Annoncer les festivals de la saison", "Announce the season's festivals" },
            { "Trier le sac à dos", "Sort the backpack" },
            { "Résumé du contenu du sac", "Summary of the bag's contents" },
            { "Ranger dans les coffres proches", "Put away in nearby chests" },
            { "Lire la description complète", "Read the full description" },
            { "Activer/désactiver le curseur libre", "Toggle the free cursor" },
            { "Curseur libre : recentrer sur soi", "Free cursor: recentre on yourself" },
            { "Placement : état courant", "Placement: current state" },
            { "Animaux : bilan du troupeau", "Animals: herd summary" },
            { "Paquet : ce qu'il manque", "Bundle: what is missing" },
            { "Panneau d'affichage : tâches du jour", "Bulletin board: today's tasks" },
            { "Sorts équipés", "Equipped spells" },
            { "Choisir un sort à équiper", "Choose a spell to equip" },
            { "Réglages (liste)", "Settings (list)" },
            { "Sauvegardes (liste)", "Saves (list)" },

            // Les descriptions de ces mêmes actions, lues juste après le libellé dans le menu des
            // raccourcis. Elles restent écrites en français dans ModConfig parce qu'elles servent
            // aussi de commentaires dans le fichier de configuration, lui-même français.
            { "Ouvre le menu d'aide, parcourable rubrique par rubrique aux flèches.",
              "Opens the help menu, browsable topic by topic with the arrows." },
            { "Décrit la case devant vous (terrain, objet, PNJ...). Non assignée par défaut : cette case est déjà décrite à chaque pas.",
              "Describes the tile in front of you (terrain, object, NPC...). Unassigned by default: that tile is already described at every step." },
            { "Annonce votre position et la direction regardée.",
              "Announces your position and the direction you are facing." },
            { "Passe au personnage proche suivant et l'annonce (nom, direction, distance).",
              "Moves to the next nearby character and announces it (name, direction, distance)." },
            { "Répète la dernière chose annoncée.", "Repeats the last thing announced." },
            { "Active ou désactive l'annonce automatique de la case devant vous en marchant.",
              "Turns the automatic announcement of the tile ahead on or off while walking." },
            { "Élément précédent dans un menu (Bas/Droite fonctionnent aussi en alternative).",
              "Previous item in a menu (Down and Right also work as alternatives)." },
            { "Élément suivant dans un menu (Haut/Gauche fonctionnent aussi en alternative).",
              "Next item in a menu (Up and Left also work as alternatives)." },
            { "Active (clic gauche) l'élément de menu actuellement annoncé. Ctrl+cette touche = clic droit.",
              "Activates (left click) the menu item currently announced. Ctrl plus this key is a right click." },
            { "Tourne votre personnage vers la gauche sans vous déplacer.",
              "Turns your character left without moving you." },
            { "Tourne votre personnage vers la droite sans vous déplacer.",
              "Turns your character right without moving you." },
            { "Diagnostic : joue un son Windows indépendant de la synthèse vocale.",
              "Diagnostic: plays a Windows sound independent of speech." },
            { "Active ou désactive la souris qui pointe toujours vers la case devant vous.",
              "Turns on or off the mouse that always points at the tile in front of you." },
            { "Simule un clic gauche à la position actuelle de la souris.",
              "Simulates a left click at the mouse's current position." },
            { "Simule un clic droit (avec Ctrl enfoncé ; Slash fonctionne aussi en alternative sur QWERTY).",
              "Simulates a right click (with Ctrl held; Slash also works as an alternative on QWERTY)." },
            { "Ouvre ou ferme le menu listant tous les raccourcis et permettant de les modifier.",
              "Opens or closes the menu listing every shortcut and letting you change them." },
            { "Élément précédent du scanner. Avec Ctrl : catégorie précédente (PNJ, plantations, ressources, bâtiments...).",
              "Previous scanner item. With Ctrl: previous category (NPCs, crops, resources, buildings...)." },
            { "Élément suivant du scanner. Avec Ctrl : catégorie suivante.",
              "Next scanner item. With Ctrl: next category." },
            { "Annonce l'élément actuellement sélectionné par le scanner (ou le plus proche si aucun). Avec Ctrl : lance un cheminement automatique vers lui (Échap pour annuler).",
              "Announces the item currently selected in the scanner (or the nearest if none). With Ctrl: starts walking to it automatically (Escape to cancel)." },
            { "Annonce le nombre d'éléments trouvés dans la catégorie actuelle du scanner. Non assignée par défaut : ce nombre est déjà annoncé au changement de catégorie.",
              "Announces how many items were found in the scanner's current category. Unassigned by default: that count is already announced when changing category." },
            { "Touche pour ouvrir le tchat/console du jeu (remplace Entrée, qui entrait en conflit avec la validation de menu).",
              "Key to open the game's chat or console (replaces Enter, which clashed with confirming in menus)." },
            { "Annonce l'heure, le jour, la saison, l'année et la météo actuelle.",
              "Announces the time, day, season, year and current weather." },
            { "Annonce la santé et le mana actuels.", "Announces current health and mana." },
            { "Active ou désactive le bip continu de visée pendant le mini-jeu de pêche.",
              "Turns the continuous aiming beep during the fishing mini-game on or off." },
            { "Annonce toutes vos quêtes actives : nom, progression, où les rendre.",
              "Announces all your active quests: name, progress, where to hand them in." },
            { "Annonce vos relations avec les PNJ romançables : cœurs, statut (en couple/marié).",
              "Announces your relationships with romanceable NPCs: hearts, status (dating or married)." },
            { "Annonce vos niveaux de compétence (combat, agriculture, pêche, minage, exploration).",
              "Announces your skill levels (combat, farming, fishing, mining, exploration)." },
            { "Lieu précédent sur la carte du monde (carte ouverte uniquement).",
              "Previous location on the world map (only with the map open)." },
            { "Lieu suivant sur la carte du monde (carte ouverte uniquement).",
              "Next location on the world map (only with the map open)." },
            { "Option précédente dans la catégorie d'apparence actuelle. Avec Contrôle : catégorie précédente (création de personnage uniquement).",
              "Previous option in the current appearance category. With Control: previous category (character creation only)." },
            { "Option suivante dans la catégorie d'apparence actuelle. Avec Contrôle : catégorie suivante (création de personnage uniquement).",
              "Next option in the current appearance category. With Control: next category (character creation only)." },
            { "Ouvre l'arbre de compétences en liste : les métiers, puis les compétences du métier choisi.",
              "Opens the skill tree as a list: the professions, then the chosen profession's skills." },
            { "Annonce les festivals de la saison actuelle (nom, jour, description).",
              "Announces the current season's festivals (name, day, description)." },
            { "Trie et regroupe le sac à dos (sans toucher à la barre d'action ni à l'équipement).",
              "Sorts and stacks the backpack (leaving the action bar and equipment alone)." },
            { "Annonce tout ce que contient le sac à dos, regroupé par objet, et le nombre d'emplacements libres.",
              "Announces everything in the backpack, grouped by item, and how many slots are free." },
            { "Range dans les coffres proches tout ce dont ils contiennent déjà un exemplaire.",
              "Puts away in nearby chests everything they already hold one of." },
            { "Relit la description complète du dernier objet annoncé (utile quand le mode bref est actif).",
              "Reads out the full description of the last item announced (useful when brief mode is on)." },
            { "Active ou désactive le curseur de case libre, déplaçable aux flèches partout sur la carte.",
              "Turns the free tile cursor on or off — movable with the arrows anywhere on the map." },
            { "Ramène le curseur libre sur la case où vous vous tenez.",
              "Brings the free cursor back to the tile you are standing on." },
            { "Redit où en est le placement en cours : objet, emprise et validité de l'emplacement visé.",
              "Repeats where the placement in progress stands: item, footprint, and whether the targeted spot is valid." },
            { "Bilan des animaux présents : combien sont à nourrir, à caresser, et combien ont laissé un produit au sol.",
              "Summary of the animals present: how many need feeding, how many need petting, and how many left a product on the ground." },
            { "Paquet ouvert : dit ce qu'il manque encore, objet par objet, avec les quantités déjà déposées.",
              "With a bundle open: says what is still missing, item by item, with the quantities already handed in." },
            { "Près d'un panneau d'affichage : annonce les tâches du jour, si elles sont déjà acceptées, et leur récompense.",
              "Near a bulletin board: announces the day's tasks, whether they are already accepted, and what they pay." },
            { "Annonce les quatre sorts actuellement équipés, emplacement par emplacement.",
              "Announces the four currently equipped spells, slot by slot." },
            { "Ouvre au clavier le choix de sort d'un emplacement. Chaque appui passe à l'emplacement suivant, puis referme.",
              "Opens a slot's spell picker from the keyboard. Each press moves to the next slot, then closes." },
            { "Ouvre les réglages en liste : ceux du mod, puis ceux du jeu affichés à l'écran. Gauche et droite changent la valeur.",
              "Opens the settings as a list: the mod's own, then the game's as shown on screen. Left and right change the value." },
            { "Sur l'écran de choix de personnage : ouvre les sauvegardes en liste, puis Charger ou Supprimer.",
              "On the character select screen: opens the saves as a list, then Load or Delete." },

            // Mana, souris et pêche : des phrases entières, donc traduites ici plutôt qu'à la
            // source, où elles sont rangées dans des tables ou des ternaires.
            { "Mana à la moitié.", "Mana at half." },
            { "Mana à un quart.", "Mana at a quarter." },
            { "Mana presque épuisé.", "Mana nearly out." },
            { "Souris directionnelle activée : elle pointe désormais vers la case devant vous.",
              "Directional mouse on: it now points at the tile in front of you." },
            { "Souris directionnelle désactivée, vous pouvez la déplacer librement.",
              "Directional mouse off, you can move it freely." },
            { "Bip de visée en pêche activé.", "Fishing aim beep on." },
            { "Bip de visée en pêche désactivé.", "Fishing aim beep off." },
            { "Manqué.", "Missed." },
            { "Touché !", "Hooked!" },
            { "Entrée.", "Entrance." },
            { "Entrée, votre maison.", "Entrance, your house." },
            { "En selle.", "Mounted." },
            { "À pied.", "On foot." },

            // Les zones du menu principal, annoncées au saut d'une zone à l'autre.
            { "Onglets", "Tabs" },
            { "Équipement", "Equipment" },
            { "Barre d'action", "Action bar" },
            { "Coffre", "Chest" },

            // Les catégories d'apparence en création de personnage. Elles sont annoncées seules
            // au changement de catégorie, donc la table suffit.
            { "Corps", "Body" },
            { "Cheveux", "Hair" },
            { "Yeux", "Eyes" },
            { "Visage", "Face" },
            { "Torse", "Chest" },
            { "Jambes", "Legs" },
            { "Tête", "Head" },
            { "Queue", "Tail" },

            // Calendrier
            { "Le calendrier n'est pas disponible pour le moment.", "The calendar is not available right now." },
            { "Aucun festival prévu cette saison.", "No festival scheduled this season." },
            { "Aucune information de festival disponible pour cette saison.",
              "No festival information available for this season." },
        };

        /// <summary>
        /// Phrases construites à l'exécution, reconnues par motif. Les groupes capturés — nombres,
        /// noms d'objets, de lieux ou de personnages — sont reportés tels quels : ils viennent du
        /// jeu, qui les a déjà donnés dans SA langue, donc en anglais quand on joue en anglais.
        ///
        /// L'ordre compte : le premier motif qui reconnaît la phrase gagne, donc les plus précis
        /// viennent avant les plus larges.
        /// </summary>
        private static readonly List<KeyValuePair<Regex, string>> Patterns =
            new List<KeyValuePair<Regex, string>>
        {
            // Combat et état
            P(@"^Touché ! Santé : (\d+) sur (\d+)\.(.*)$",       "Hit! Health: $1 of $2.$3"),
            P(@"^Santé : (\d+) sur (\d+)\.$",                    "Health: $1 of $2."),
            P(@"^Mana : (\d+) sur (\d+)\.$",                     "Mana: $1 of $2."),
            P(@"^(.+) vaincu\.$",                                "$1 defeated."),
            P(@"^Donjon de combat, étage (\d+)\.$",              "Combat dungeon, floor $1."),

            // Autres joueurs
            P(@"^(.+) a rejoint la partie\.$",                   "$1 joined the game."),
            P(@"^(.+) a quitté la partie\.$",                    "$1 left the game."),

            // Sauvegardes
            P(@"^Chargement de (.+)\.$",                         "Loading $1."),
            P(@"^Suppression de (.+)\.$",                        "Deleting $1."),
            P(@"^Charger la partie de (.+)$",                    "Load $1's game"),
            P(@"^Supprimer la partie de (.+)$",                  "Delete $1's game"),
            P(@"^Détails complets de (.+)$",                     "Full details for $1"),
            P(@"^Que faire pour (.+)$",                          "What to do for $1"),
            P(@"^Détails de (.+)$",                              "Details for $1"),
            P(@"^Emplacement vide$",                             "Empty slot"),
            P(@"^Entrée, (.+)\.$",                               "Entrance, $1."),
            P(@"^Nouvelle partie$",                              "New game"),

            // Cheminement automatique
            P(@"^Cheminement vers (.+)\.$",                      "Walking to $1."),
            P(@"^Arrivé près de (.+)\.$",                        "Arrived near $1."),
            P(@"^Chemin bloqué avant (.+) : approche au maximum\.$",
                                                                 "Path blocked before $1: got as close as possible."),
            P(@"^Impossible de bouger vers (.+), le passage est bloqué\.$",
                                                                 "Cannot move towards $1, the way is blocked."),
            P(@"^(.+) est trop loin pour un cheminement automatique\.$",
                                                                 "$1 is too far for auto-pathing."),

            // Curseur libre
            P(@"^Curseur libre activé\. (.*)$",                  "Free cursor on. $1"),
            P(@"^Recentré\. (.*)$",                              "Recentred. $1"),

            // Listes et menus
            P(@"^(\d+) sur (\d+)\. (.*)$",                       "$1 of $2. $3"),
            P(@"^(.+) fermé\.$",                                 "$1 closed."),
            P(@"^(.+) : rien à afficher\.$",                     "$1: nothing to show."),
            P(@"^(.+) : aucune compétence trouvée\.$",           "$1: no skill found."),
            P(@"^Rien trouvé en (.+)\.$",                        "Nothing found in $1."),
            P(@"^Emplacement (\d+) : aucun sort disponible\.$",  "Slot $1: no spell available."),

            // Quêtes
            P(@"^Nouvelle quête : (.+)\.$",                      "New quest: $1."),
            P(@"^Quête terminée : (.+)\.$",                      "Quest complete: $1."),

            // Raccourcis
            P(@"^Touche pour (.+) changée en (.+)\.$",           "Key for $1 changed to $2."),
            P(@"^(.+) n'est plus assigné à aucune touche\.$",    "$1 is no longer bound to any key."),
        };

        private static KeyValuePair<Regex, string> P(string pattern, string replacement) =>
            new KeyValuePair<Regex, string>(new Regex(pattern), replacement);
    }
}
