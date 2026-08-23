using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// Beaucoup de boutons d'interface (icônes seules, onglets) n'ont aucun texte affiché : le
    /// jeu ne leur donne un sens que visuellement. Dans ce cas, on retombe sur le nom Unity
    /// interne de l'objet — presque toujours en anglais et illisible tel quel ("Btn_CloseIcon",
    /// "InventoryTab(Clone)"...). Ce traducteur découpe ce nom en mots et traduit ceux qu'il
    /// reconnaît, pour obtenir quelque chose de compréhensible plutôt qu'un nom de variable.
    /// Best-effort : ne remplace jamais un vrai texte du jeu (déjà en français), ne sert qu'en
    /// tout dernier repli.
    /// </summary>
    public static class UiNameTranslator
    {
        private static readonly Dictionary<string, string> Dictionary = new Dictionary<string, string>
        {
            // Actions génériques
            ["play"] = "Jouer", ["continue"] = "Continuer", ["new"] = "Nouveau",
            ["load"] = "Charger", ["save"] = "Sauvegarder", ["delete"] = "Supprimer",
            ["back"] = "Retour", ["close"] = "Fermer", ["exit"] = "Quitter", ["quit"] = "Quitter",
            ["confirm"] = "Confirmer", ["cancel"] = "Annuler", ["accept"] = "Accepter",
            ["decline"] = "Refuser", ["ok"] = "Valider", ["yes"] = "Oui", ["no"] = "Non",
            ["apply"] = "Appliquer", ["reset"] = "Réinitialiser", ["default"] = "Par défaut",
            ["submit"] = "Envoyer", ["search"] = "Rechercher", ["filter"] = "Filtrer",
            ["sort"] = "Trier", ["next"] = "Suivant", ["previous"] = "Précédent",
            ["prev"] = "Précédent", ["toggle"] = "Basculer", ["enable"] = "Activer",
            ["disable"] = "Désactiver", ["rename"] = "Renommer", ["select"] = "Sélectionner",
            ["remove"] = "Retirer", ["add"] = "Ajouter", ["create"] = "Créer",
            ["edit"] = "Modifier", ["open"] = "Ouvrir",

            // Écrans / onglets
            ["tab"] = "onglet", ["menu"] = "menu", ["panel"] = "panneau",
            ["inventory"] = "Inventaire", ["map"] = "Carte", ["quest"] = "Quêtes",
            ["journal"] = "Journal", ["skills"] = "Compétences", ["skill"] = "Compétence",
            ["crafting"] = "Artisanat", ["craft"] = "Fabriquer", ["cook"] = "Cuisiner",
            ["cooking"] = "Cuisine", ["fish"] = "Pêche", ["fishing"] = "Pêche",
            ["mine"] = "Mine", ["mining"] = "Minage", ["farm"] = "Ferme", ["farming"] = "Agriculture",
            ["shop"] = "Boutique", ["sell"] = "Vendre", ["buy"] = "Acheter", ["trade"] = "Échanger",
            ["equip"] = "Équiper", ["unequip"] = "Déséquiper", ["wear"] = "Porter",
            ["use"] = "Utiliser", ["drop"] = "Jeter", ["pickup"] = "Ramasser",
            ["chest"] = "Coffre", ["mailbox"] = "Boîte aux lettres", ["mail"] = "Courrier",
            ["friends"] = "Amis", ["relationship"] = "Relation", ["marriage"] = "Mariage",
            ["wedding"] = "Mariage", ["character"] = "Personnage", ["customize"] = "Personnaliser",
            ["hair"] = "Cheveux", ["skin"] = "Peau", ["eyes"] = "Yeux", ["clothes"] = "Vêtements",
            ["pet"] = "Animal de compagnie", ["animal"] = "Animal", ["house"] = "Maison",
            ["home"] = "Maison", ["upgrade"] = "Améliorer", ["build"] = "Construire",
            ["place"] = "Placer", ["achievements"] = "Succès", ["stats"] = "Statistiques",
            ["settings"] = "Paramètres", ["options"] = "Options", ["audio"] = "Audio",
            ["video"] = "Vidéo", ["controls"] = "Contrôles", ["graphics"] = "Graphismes",
            ["language"] = "Langue", ["volume"] = "Volume", ["music"] = "Musique",
            ["sfx"] = "Effets sonores", ["fullscreen"] = "Plein écran", ["windowed"] = "Fenêtré",
            ["resolution"] = "Résolution", ["display"] = "Affichage", ["gameplay"] = "Jouabilité",
            ["profile"] = "Profil", ["party"] = "Groupe", ["social"] = "Social",
            ["notification"] = "Notification", ["notifications"] = "Notifications",

            // Mots vides à ignorer plutôt qu'à traduire littéralement
            ["btn"] = "", ["button"] = "", ["icon"] = "", ["img"] = "", ["image"] = "",
            ["ui"] = "", ["obj"] = "", ["object"] = "", ["clone"] = "", ["gameobject"] = "",
            ["container"] = "", ["group"] = "", ["holder"] = "", ["wrapper"] = "",
            ["background"] = "", ["bg"] = "", ["prefab"] = "",

            // Bâtiments de ferme (Wish.PortalType, entrées de bâtiments du scanner)
            ["normal"] = "Bâtiment", ["barn"] = "Grange", ["coop"] = "Poulailler",
            ["warehouse"] = "Entrepôt", ["workshop"] = "Atelier", ["nelvari"] = "Nelvari",
            ["withergate"] = "Withergate", ["garden"] = "Jardin", ["shed"] = "Remise",
            ["greenhouse"] = "Serre", ["player"] = "Joueur",

            // Noms de scènes fréquents dans sceneToLoadString (boutiques, PNJ, donjons)
            ["store"] = "Boutique", ["generalstore"] = "Magasin général",
            ["blacksmith"] = "Forgeron", ["tavern"] = "Taverne", ["bar"] = "Bar",
            ["guild"] = "Guilde", ["adventurers"] = "Aventuriers", ["temple"] = "Temple",
            ["dungeon"] = "Donjon", ["cave"] = "Grotte", ["mine"] = "Mine",
            ["sewer"] = "Égout", ["vault"] = "Coffre-fort", ["apartment"] = "Appartement",
            ["clinic"] = "Clinique", ["library"] = "Bibliothèque", ["school"] = "École",
            ["townhall"] = "Mairie", ["bank"] = "Banque", ["inn"] = "Auberge",

            // Géographie / nature (noms de zones et de cartes du monde)
            ["beach"] = "Plage", ["hunting"] = "Chasse", ["ground"] = "Terrain",
            ["grounds"] = "Terrain", ["forest"] = "Forêt", ["woods"] = "Bois",
            ["grove"] = "Bosquet", ["meadow"] = "Prairie", ["field"] = "Champ",
            ["fields"] = "Champs", ["plains"] = "Plaines", ["valley"] = "Vallée",
            ["hill"] = "Colline", ["hills"] = "Collines", ["mountain"] = "Montagne",
            ["mountains"] = "Montagnes", ["peak"] = "Sommet", ["ridge"] = "Crête",
            ["cliff"] = "Falaise", ["cliffs"] = "Falaises", ["canyon"] = "Canyon",
            ["desert"] = "Désert", ["oasis"] = "Oasis", ["swamp"] = "Marécage",
            ["marsh"] = "Marais", ["jungle"] = "Jungle", ["tundra"] = "Toundra",
            ["glacier"] = "Glacier", ["frozen"] = "Gelé", ["snow"] = "Neige",
            ["ice"] = "Glace", ["volcano"] = "Volcan", ["lava"] = "Lave",
            ["lake"] = "Lac", ["river"] = "Rivière", ["stream"] = "Ruisseau",
            ["pond"] = "Étang", ["spring"] = "Source", ["falls"] = "Chutes",
            ["waterfall"] = "Cascade", ["bridge"] = "Pont", ["path"] = "Chemin",
            ["trail"] = "Sentier", ["road"] = "Route", ["crossroads"] = "Carrefour",
            ["cove"] = "Crique", ["bay"] = "Baie", ["shore"] = "Rivage",
            ["coast"] = "Côte", ["isle"] = "Île", ["island"] = "Île",
            ["reef"] = "Récif", ["docks"] = "Quais", ["harbor"] = "Port",
            ["port"] = "Port", ["pier"] = "Jetée", ["orchard"] = "Verger",
            ["greenhouse2"] = "Serre",

            // Bâtiments / lieux construits fréquents
            ["ruins"] = "Ruines", ["camp"] = "Camp", ["outpost"] = "Avant-poste",
            ["settlement"] = "Campement", ["village"] = "Village", ["city"] = "Ville",
            ["town"] = "Ville", ["district"] = "Quartier", ["market"] = "Marché",
            ["plaza"] = "Place", ["square"] = "Place", ["tower"] = "Tour",
            ["fortress"] = "Forteresse", ["castle"] = "Château", ["keep"] = "Donjon",
            ["shrine"] = "Sanctuaire", ["sanctuary"] = "Sanctuaire", ["altar"] = "Autel",
            ["graveyard"] = "Cimetière", ["cemetery"] = "Cimetière", ["stable"] = "Écurie",
            ["kennel"] = "Chenil", ["aviary"] = "Volière", ["well"] = "Puits",
            ["arena"] = "Arène", ["stadium"] = "Stade", ["theater"] = "Théâtre",
            ["museum"] = "Musée", ["hospital"] = "Hôpital", ["prison"] = "Prison",
            ["lighthouse"] = "Phare", ["windmill"] = "Moulin", ["mill"] = "Moulin",
            ["barracks"] = "Caserne", ["chapel"] = "Chapelle", ["church"] = "Église",
            ["cathedral"] = "Cathédrale", ["throne"] = "Trône", ["room"] = "Salle",
            ["hall"] = "Salle", ["chamber"] = "Chambre", ["basement"] = "Sous-sol",
            ["attic"] = "Grenier", ["rooftop"] = "Toit", ["yard"] = "Cour",
            ["backyard"] = "Arrière-cour", ["courtyard"] = "Cour intérieure",

            // Adjectifs fréquents dans les noms de zones/donjons
            ["old"] = "Vieux", ["ancient"] = "Ancien", ["hidden"] = "Caché",
            ["secret"] = "Secret", ["lost"] = "Perdu", ["forgotten"] = "Oublié",
            ["sunken"] = "Englouti", ["abandoned"] = "Abandonné", ["haunted"] = "Hanté",
            ["cursed"] = "Maudit", ["sacred"] = "Sacré", ["golden"] = "Doré",
            ["silver"] = "Argenté", ["iron"] = "De fer", ["crystal"] = "De cristal",
            ["shadow"] = "De l'ombre", ["shadows"] = "Des ombres", ["light"] = "De lumière",
            ["dark"] = "Sombre", ["deep"] = "Profond", ["upper"] = "Supérieur",
            ["lower"] = "Inférieur", ["north"] = "Nord", ["south"] = "Sud",
            ["east"] = "Est", ["west"] = "Ouest", ["great"] = "Grand",
            ["small"] = "Petit", ["big"] = "Grand", ["new"] = "Nouveau",

            // Autres mots fréquents dans les noms internes (dont catégories du scanner)
            ["chicken"] = "Poule", ["cow"] = "Vache", ["sheep"] = "Mouton",
            ["pig"] = "Cochon", ["horse"] = "Cheval", ["fox"] = "Renard",
            ["cat"] = "Chat", ["dog"] = "Chien", ["monster"] = "Monstre",
            ["enemy"] = "Ennemi", ["boss"] = "Boss", ["bed"] = "Lit",
            ["furniture"] = "Meuble", ["storage"] = "Rangement", ["main"] = "Principal",
        };

        /// <summary>
        /// Repli mot-par-mot mal adapté à certains noms composés fréquents : "MainMenuToggle"
        /// se traduisait en "Principal menu Basculer" (ordre des mots faux, sens perdu — rapporté
        /// par l'utilisateur comme lu d'un bloc, "mainmenubasculer"). Ces noms exacts (une fois le
        /// suffixe de clonage Unity retiré, insensible à la casse) sont vérifiés AVANT le
        /// découpage mot-par-mot, pour un vrai libellé français au lieu d'une traduction littérale
        /// dans le mauvais ordre.
        /// </summary>
        private static readonly Dictionary<string, string> ExactPhrases = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["MainMenuToggle"] = "Retour au menu principal",
            ["MainMenuButton"] = "Retour au menu principal",
            ["ToggleMainMenu"] = "Retour au menu principal",
            ["MainMenu"] = "Menu principal",
            ["QuitToMainMenu"] = "Quitter vers le menu principal",
            ["ReturnToMainMenu"] = "Retour au menu principal",
            ["BackToMainMenu"] = "Retour au menu principal",
        };

        // Uniquement le suffixe "(1)" que Unity ajoute aux doublons de nom — surtout PAS un
        // chiffre collé au nom lui-même (ex. "MajorTab1"..."MajorTab7") : ce chiffre est
        // souvent la seule chose qui distingue des éléments par ailleurs identiques (onglets
        // numérotés), et le supprimer aveuglément les rendait tous indiscernables.
        private static readonly Regex CloneSuffix = new Regex(@"\(\d+\)$");

        /// <summary>
        /// Libellés confirmés du menu principal (touche Tab), 7 onglets nommés "MajorTabN" sans
        /// aucun texte visible. Le chiffre N dans le nom brut s'est révélé peu fiable (décalage
        /// constaté selon l'indexation interne du jeu) : la correspondance se fait donc par
        /// POSITION réelle parmi les onglets "Major" (1 = le plus à gauche/premier de la
        /// hiérarchie), pas par le chiffre du nom — voir UiTextExtractor.TryMajorTabLabel.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string> MajorTabLabelsByRank = new Dictionary<int, string>
        {
            [1] = "Sac à dos",
            [2] = "Arbre de compétences",
            [3] = "Relations",
            [4] = "Quêtes",
            [5] = "Carte",
            [6] = "Statistiques",
            [7] = "Paramètres",
        };

        public static bool IsMajorTabName(string rawName) =>
            !string.IsNullOrEmpty(rawName) && rawName.StartsWith("Major", System.StringComparison.OrdinalIgnoreCase);

        public static string Translate(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return rawName;

            string cleaned = CloneSuffix.Replace(rawName, "").Replace("(Clone)", "").Trim();
            if (ExactPhrases.TryGetValue(cleaned, out string exact)) return exact;

            string words = SplitIntoWords(rawName);
            var tokens = words.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            var result = new List<string>();
            foreach (string token in tokens)
            {
                string key = token.ToLowerInvariant();
                if (Dictionary.TryGetValue(key, out string translated))
                {
                    if (!string.IsNullOrEmpty(translated)) result.Add(translated);
                }
                else
                {
                    result.Add(token);
                }
            }

            if (result.Count == 0) return rawName;
            return string.Join(" ", result);
        }

        /// <summary>CamelCase/PascalCase/snake_case/chiffres collés -> mots séparés par des espaces.</summary>
        private static string SplitIntoWords(string name)
        {
            string s = CloneSuffix.Replace(name, "");
            s = s.Replace("(Clone)", "");
            s = s.Replace('_', ' ').Replace('-', ' ').Replace('.', ' ');

            var sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool upperBoundary = char.IsUpper(c) && i > 0 &&
                    (char.IsLower(s[i - 1]) || (char.IsUpper(s[i - 1]) && i + 1 < s.Length && char.IsLower(s[i + 1])));
                bool digitBoundary = i > 0 &&
                    ((char.IsDigit(c) && !char.IsDigit(s[i - 1])) || (!char.IsDigit(c) && char.IsDigit(s[i - 1]) && c != ' '));
                if (upperBoundary || digitBoundary)
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
