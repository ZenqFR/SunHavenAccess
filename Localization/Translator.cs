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

        private static readonly Regex GroupReference = new Regex(@"\$(\d+)", RegexOptions.Compiled);

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
            { "Bâtiments et portails", "Buildings and portals" },
            { "Animaux et compagnons", "Animals and pets" },
            { "Ennemis", "Enemies" },
            { "Mobilier et rangement", "Furniture and storage" },

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
            new KeyValuePair<Regex, string>(new Regex(pattern, RegexOptions.Compiled), replacement);
    }
}
