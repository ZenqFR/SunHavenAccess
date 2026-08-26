using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace SunHavenAccess.Config
{
    /// <summary>
    /// Toutes les touches du mod, personnalisables après le premier lancement dans
    /// BepInEx/config/com.kleitz.sunhavenaccess.cfg — ou directement en jeu via le menu des
    /// raccourcis (voir Menus/ShortcutsMenu.cs). Chaque action n'a qu'UNE touche configurable,
    /// vérifiée à la fois via UnityEngine.Input et via Rewired (voir Input/HotkeyManager.cs)
    /// pour rester fiable quel que soit l'état de Verr. Num ou la façon dont le jeu capte le
    /// clavier. Deux combinaisons Ctrl+touche (clic droit dans le monde, action secondaire de
    /// menu) sont gérées à part dans HotkeyManager : ce système de config ne capte qu'une seule
    /// touche sans modificateur par action, pas de combinaison.
    /// </summary>
    public static class ModConfig
    {
        public static ConfigEntry<KeyCode> Help;
        public static ConfigEntry<KeyCode> DescribeFront;
        public static ConfigEntry<KeyCode> Position;
        public static ConfigEntry<KeyCode> NextNpc;
        public static ConfigEntry<KeyCode> Repeat;
        public static ConfigEntry<KeyCode> ToggleVerbosity;
        public static ConfigEntry<KeyCode> MenuPrevious;
        public static ConfigEntry<KeyCode> MenuNext;
        public static ConfigEntry<KeyCode> MenuActivate;
        public static ConfigEntry<KeyCode> TurnLeft;
        public static ConfigEntry<KeyCode> TurnRight;
        public static ConfigEntry<KeyCode> TestTone;
        public static ConfigEntry<KeyCode> MouseFollowToggle;
        public static ConfigEntry<KeyCode> SimulateLeftClick;
        public static ConfigEntry<KeyCode> SimulateRightClick;
        public static ConfigEntry<KeyCode> ShortcutsMenuToggle;
        public static ConfigEntry<KeyCode> ScannerPageUp;
        public static ConfigEntry<KeyCode> ScannerPageDown;
        public static ConfigEntry<KeyCode> ScannerNearest;
        public static ConfigEntry<KeyCode> ScannerCount;
        public static ConfigEntry<KeyCode> ChatOpenKey;
        public static ConfigEntry<KeyCode> Clock;
        public static ConfigEntry<KeyCode> Status;
        public static ConfigEntry<KeyCode> FishingToneToggle;
        public static ConfigEntry<KeyCode> AnnounceQuests;
        public static ConfigEntry<KeyCode> AnnounceRelationships;
        public static ConfigEntry<KeyCode> AnnounceProfessions;
        public static ConfigEntry<KeyCode> MapPreviousLocation;
        public static ConfigEntry<KeyCode> MapNextLocation;
        public static ConfigEntry<KeyCode> AppearancePrevious;
        public static ConfigEntry<KeyCode> AppearanceNext;
        public static ConfigEntry<KeyCode> AnnounceSkillPoints;
        public static ConfigEntry<KeyCode> AnnounceFestivals;

        /// <summary>Réglages hors touches (cases à cocher), voir Init.</summary>
        public static ConfigEntry<bool> EdgeSound;
        public static ConfigEntry<bool> TakeOverMenuArrows;
        public static ConfigEntry<bool> BriefMode;

        public static ConfigEntry<KeyCode> SortBackpack;
        public static ConfigEntry<KeyCode> AnnounceContents;
        public static ConfigEntry<KeyCode> StoreInChests;
        public static ConfigEntry<KeyCode> ReadFullDescription;
        public static ConfigEntry<KeyCode> FreeCursorToggle;
        public static ConfigEntry<KeyCode> FreeCursorRecenter;
        public static ConfigEntry<KeyCode> PlacementStatus;
        public static ConfigEntry<KeyCode> HerdStatus;
        public static ConfigEntry<KeyCode> BundleStatus;
        public static ConfigEntry<KeyCode> BulletinBoardTasks;
        public static ConfigEntry<KeyCode> AnnounceSpells;
        public static ConfigEntry<KeyCode> EquipSpell;

        /// <summary>Libellé + entrée, pour le menu des raccourcis (parcourable/modifiable en jeu).</summary>
        public static readonly List<(string Label, ConfigEntry<KeyCode> Entry)> All =
            new List<(string, ConfigEntry<KeyCode>)>();

        public static void Init(ConfigFile config)
        {
            const string section = "Touches";

            // IMPORTANT (23/08/2026) : Wish.UserSettings.DefaultKeybinds (décompilé) montre que
            // F1 à F7 sont TOUTES les touches d'émote par défaut du jeu (Emote1..Emote7) — donc
            // F2/F3/F4/F6/F7, utilisées ici jusque-là, déclenchaient CHACUNE une émote du
            // personnage en plus de l'action du mod. Unity ne fait aucune distinction de
            // modificateur sur Input.GetKeyDown : impossible de "protéger" une touche F1-F7 avec
            // Ctrl/Maj, le jeu la voit quand même. Reclassées sur des lettres/touches libres
            // (absentes de DefaultKeybinds) — clés de config renommées pour forcer les nouveaux
            // défauts, mêmes anciennes valeurs encore utilisables en repli si déjà personnalisées
            // à la main par l'utilisateur.
            // ATTENTION : F1 est l'émote 1 par défaut du jeu (Wish.UserSettings.DefaultKeybinds,
            // Emote1) — exactement le genre de conflit que H avait corrigé. Remis sur F1 sur
            // demande explicite de l'utilisateur malgré ça : la touche d'aide déclenchera aussi
            // une émote du personnage à chaque pression.
            Help = Bind(config, section, "AideV3", KeyCode.F1,
                "Ouvre le menu d'aide, parcourable rubrique par rubrique aux flèches.", "Aide");
            DescribeFront = Bind(config, section, "CaseDevantVousV2", KeyCode.F10,
                "Décrit la case devant vous (terrain, objet, PNJ...).", "Décrire la case devant vous");
            Position = Bind(config, section, "PositionV2", KeyCode.P,
                "Annonce votre position et la direction regardée.", "Annoncer votre position");
            NextNpc = Bind(config, section, "PersonnageProcheV2", KeyCode.N,
                "Passe au personnage proche suivant et l'annonce (nom, direction, distance).", "Personnage proche suivant");
            Repeat = Bind(config, section, "RepeterV2", KeyCode.B,
                "Répète la dernière chose annoncée.", "Répéter la dernière annonce");
            ToggleVerbosity = Bind(config, section, "VerbositeDeplacement", KeyCode.F9,
                "Active ou désactive l'annonce automatique de la case devant vous en marchant.", "Verbosité des déplacements");

            // Navigation de menu : flèches directionnelles + Entrée, comme demandé. Ctrl+Entrée
            // (action secondaire / clic droit) est géré directement dans HotkeyManager.
            MenuPrevious = Bind(config, section, "MenuPrecedent", KeyCode.UpArrow,
                "Élément précédent dans un menu (Bas/Droite fonctionnent aussi en alternative).", "Élément de menu précédent");
            MenuNext = Bind(config, section, "MenuSuivant", KeyCode.DownArrow,
                "Élément suivant dans un menu (Haut/Gauche fonctionnent aussi en alternative).", "Élément de menu suivant");
            MenuActivate = Bind(config, section, "MenuValider", KeyCode.Return,
                "Active (clic gauche) l'élément de menu actuellement annoncé. Ctrl+cette touche = clic droit.", "Activer l'élément de menu (clic gauche)");

            TurnLeft = Bind(config, section, "TournerGauche", KeyCode.Keypad4,
                "Tourne votre personnage vers la gauche sans vous déplacer.", "Tourner à gauche sans se déplacer");
            TurnRight = Bind(config, section, "TournerDroite", KeyCode.Keypad6,
                "Tourne votre personnage vers la droite sans vous déplacer.", "Tourner à droite sans se déplacer");
            TestTone = Bind(config, section, "TestSon", KeyCode.F11,
                "Diagnostic : joue un son Windows indépendant de la synthèse vocale.", "Test sonore (diagnostic)");
            MouseFollowToggle = Bind(config, section, "SourisDirectionnelle", KeyCode.J,
                "Active ou désactive la souris qui pointe toujours vers la case devant vous.", "Activer/désactiver la souris directionnelle");
            // Espace est en fait le SAUT par défaut du jeu (Wish.UserSettings.DefaultKeybinds,
            // Button.Jump) : le personnage aurait sauté à chaque clic simulé. Reclassé sur le
            // pavé numérique, à côté des touches de rotation (Pavé 4/6) déjà là.
            SimulateLeftClick = Bind(config, section, "ClicGaucheMondeV3", KeyCode.Keypad5,
                "Simule un clic gauche à la position actuelle de la souris.", "Simuler un clic gauche (monde)");
            SimulateRightClick = Bind(config, section, "ClicDroit", KeyCode.Colon,
                "Simule un clic droit (avec Ctrl enfoncé ; Slash fonctionne aussi en alternative sur QWERTY).", "Simuler un clic droit (monde, avec Ctrl)");
            ShortcutsMenuToggle = Bind(config, section, "MenuRaccourcisV2", KeyCode.Delete,
                "Ouvre ou ferme le menu listant tous les raccourcis et permettant de les modifier.", "Ouvrir le menu des raccourcis");

            // Scanner par catégories (Object Tracker façon stardew-access), même convention de
            // touches que la référence : Page haut/bas seules parcourent les éléments de la
            // catégorie actuelle, Ctrl+Page haut/bas change de catégorie (même touche physique,
            // Ctrl change le sens dans HotkeyManager) ; Origine annonce/sélectionne l'élément
            // actuel, Ctrl+Origine lance un cheminement automatique vers lui (Échap annule) ; Fin
            // annonce le nombre d'éléments trouvés.
            ScannerPageUp = Bind(config, section, "ScannerPageHaut", KeyCode.PageUp,
                "Élément précédent du scanner. Avec Ctrl : catégorie précédente (PNJ, plantations, ressources, bâtiments...).", "Scanner : élément précédent (Ctrl = catégorie précédente)");
            ScannerPageDown = Bind(config, section, "ScannerPageBas", KeyCode.PageDown,
                "Élément suivant du scanner. Avec Ctrl : catégorie suivante.", "Scanner : élément suivant (Ctrl = catégorie suivante)");
            ScannerNearest = Bind(config, section, "ScannerOrigine", KeyCode.Home,
                "Annonce l'élément actuellement sélectionné par le scanner (ou le plus proche si aucun). Avec Ctrl : lance un cheminement automatique vers lui (Échap pour annuler).", "Scanner : annoncer l'élément sélectionné (Ctrl = s'y rendre)");
            ScannerCount = Bind(config, section, "ScannerFin", KeyCode.End,
                "Annonce le nombre d'éléments trouvés dans la catégorie actuelle du scanner.", "Scanner : annoncer le nombre trouvé");

            // Le tchat/console de debug du jeu (Quantum Console) s'ouvrait par défaut sur Entrée,
            // en plein conflit avec la validation de menu/dialogue du mod — voir
            // Input/ChatKeyRebinder.cs, qui applique cette touche au tchat dès qu'il existe.
            // T était aussi le Sort 3 par défaut du jeu (Button.Spell3) : reclassé sur C (Chat).
            ChatOpenKey = Bind(config, section, "OuvertureTchatV2", KeyCode.C,
                "Touche pour ouvrir le tchat/console du jeu (remplace Entrée, qui entrait en conflit avec la validation de menu).", "Ouvrir le tchat / la console du jeu");

            Clock = Bind(config, section, "HorlogeV2", KeyCode.O,
                "Annonce l'heure, le jour, la saison, l'année et la météo actuelle.", "Annoncer l'heure, le jour, la saison et la météo");

            Status = Bind(config, section, "StatutV2", KeyCode.H,
                "Annonce la santé et le mana actuels.", "Annoncer la santé et le mana");

            // Fonctionnalité nouvelle et non testée en conditions réelles (bip continu de visée
            // pendant le mini-jeu de pêche, voir Speech/FishingToneCue.cs) : une touche pour le
            // couper au cas où le son ne conviendrait pas, sans devoir quitter la pêche.
            // CONFLIT CORRIGÉ (23/08/2026) : K était aussi la touche par défaut du jeu pour
            // Button.Skills (ouverture du menu Compétences, Wish.UserSettings.DefaultKeybinds)
            // — repérée en travaillant sur les quêtes, jamais recroisée avec la table depuis
            // l'ajout de cette touche. Déplacée sur F8, libre des deux côtés (jeu et mod).
            FishingToneToggle = Bind(config, section, "BipPecheV2", KeyCode.F8,
                "Active ou désactive le bip continu de visée pendant le mini-jeu de pêche.", "Activer/désactiver le bip de visée en pêche");

            // Quêtes (Wish.QuestList / Wish.Quest) : totalement absent du mod jusqu'ici. L
            // (Button.Quests par défaut) ouvre le journal de quêtes du jeu, mais son contenu
            // n'est pas lisible nativement par un lecteur d'écran (liste visuelle, pas de
            // Selectable clavier) — cette touche lit directement les données de quête, sans
            // dépendre de l'écran du journal. G : aucune touche mnémotechnique libre restante
            // ("Q" est Spell1 du jeu), simple touche libre des deux côtés.
            AnnounceQuests = Bind(config, section, "Quetes", KeyCode.G,
                "Annonce toutes vos quêtes actives : nom, progression, où les rendre.", "Annoncer les quêtes actives");

            // Relations avec les PNJ romançables (Wish.GameSave.CurrentCharacter.Relationships) :
            // le jeu affiche ça sous forme de cœurs remplis visuellement, sans texte natif
            // équivalent. V : plus de touche mnémotechnique libre à ce stade (R est Spell2 du
            // jeu), simple touche libre des deux côtés.
            AnnounceRelationships = Bind(config, section, "Relations", KeyCode.V,
                "Annonce vos relations avec les PNJ romançables : cœurs, statut (en couple/marié).", "Annoncer les relations");

            // Niveaux de compétence (Combat, Agriculture, Pêche, Minage, Exploration) — pas
            // l'arbre de compétences lui-même (grille 2D de nœuds, touche K du jeu par défaut,
            // pas encore accessible). U : dernière lettre libre simple, sans mnémonique.
            AnnounceProfessions = Bind(config, section, "Competences", KeyCode.U,
                "Annonce vos niveaux de compétence (combat, agriculture, pêche, minage, exploration).", "Annoncer les niveaux de compétence");

            // Carte du monde (Wish.Map/LocationName) : les lieux ne sont PAS de vrais Selectable
            // (voir Navigation/MapNavigator.cs) donc invisibles au scan générique de menu —
            // touches dédiées pour les parcourir un par un, actives seulement carte ouverte.
            MapPreviousLocation = Bind(config, section, "CarteLieuPrecedent", KeyCode.X,
                "Lieu précédent sur la carte du monde (carte ouverte uniquement).", "Carte : lieu précédent");
            MapNextLocation = Bind(config, section, "CarteLieuSuivant", KeyCode.Y,
                "Lieu suivant sur la carte du monde (carte ouverte uniquement).", "Carte : lieu suivant");

            // Apparence en création de personnage (Wish.ClothingImageButton, même problème que
            // la carte : pas de vrai Selectable). Convention Scanner : touche seule = option
            // dans la catégorie actuelle, Ctrl+touche = catégorie (corps/cheveux/yeux/visage/
            // torse/jambes/tête/queue). Virgule/Point : libres des deux côtés, pas de touche
            // lettre simple restante à ce stade.
            AppearancePrevious = Bind(config, section, "ApparencePrecedent", KeyCode.Comma,
                "Option précédente dans la catégorie d'apparence actuelle. Avec Contrôle : catégorie précédente (création de personnage uniquement).", "Apparence : précédent (Ctrl = catégorie)");
            AppearanceNext = Bind(config, section, "ApparenceSuivant", KeyCode.Period,
                "Option suivante dans la catégorie d'apparence actuelle. Avec Contrôle : catégorie suivante (création de personnage uniquement).", "Apparence : suivant (Ctrl = catégorie)");

            // Résumé des points de compétence par arbre — pas la navigation dans la grille de
            // nœuds elle-même (voir Info/SkillPointsAnnouncer.cs). Z : dernière lettre libre.
            AnnounceSkillPoints = Bind(config, section, "PointsCompetence", KeyCode.Z,
                "Annonce les points de compétence disponibles dans chaque arbre.", "Annoncer les points de compétence disponibles");

            // Festivals de la saison (Wish.CalendarUI) : écran purement visuel (grille de jours,
            // aucune interaction clavier native) — juste à lire, pas à naviguer. Plus aucune
            // lettre simple libre à ce stade (26/26 utilisées) : point-virgule, libre des deux côtés.
            AnnounceFestivals = Bind(config, section, "Festivals", KeyCode.Semicolon,
                "Annonce les festivals de la saison actuelle (nom, jour, description).", "Annoncer les festivals de la saison");

            // Réglages de confort, PAS des touches : volontairement hors de la liste `All` (donc
            // absents du menu vocal des raccourcis, qui ne sait réassigner que des touches).
            const string navSection = "Navigation";

            EdgeSound = config.Bind(navSection, "SonDeBord", true,
                "Joue un bip court et grave quand on bute sur le bord d'une zone (fin de ligne " +
                "d'inventaire, dernier onglet...). Mettre à false pour naviguer en silence.");

            TakeOverMenuArrows = config.Bind(navSection, "NavigationDirectionnelle", true,
                "Le mod prend la main sur les flèches dans les menus pour offrir une navigation " +
                "directionnelle réelle (ligne/colonne, zones séparées). Mettre à false pour " +
                "rendre les flèches au jeu si un écran se comporte mal (le curseur saute deux " +
                "cases d'un coup, par exemple).");

            BriefMode = config.Bind(navSection, "ModeBref", true,
                "En parcourant l'inventaire, n'annonce que la quantité et le nom de l'objet au " +
                "lieu de sa description complète (le jeu fusionne les deux dans un seul texte, " +
                "ce qui rend le parcours très long). La description reste disponible à la " +
                "demande avec la touche dédiée. Sans effet en boutique et en artisanat, où le " +
                "prix et les ingrédients sont indispensables.");

            // ---- Confort d'inventaire ----
            // Les 26 lettres étant prises (par le jeu ou par le mod), on passe par de la
            // ponctuation, vérifiée absente de Wish.UserSettings.DefaultKeybinds. Toutes
            // réassignables via le menu des raccourcis (touche Suppr) si la disposition clavier gêne.
            SortBackpack = Bind(config, section, "TrierSac", KeyCode.Quote,
                "Trie et regroupe le sac à dos (sans toucher à la barre d'action ni à l'équipement).", "Trier le sac à dos");
            AnnounceContents = Bind(config, section, "ResumeSac", KeyCode.Backslash,
                "Annonce tout ce que contient le sac à dos, regroupé par objet, et le nombre d'emplacements libres.", "Résumé du contenu du sac");
            StoreInChests = Bind(config, section, "RangerCoffres", KeyCode.Equals,
                "Range dans les coffres proches tout ce dont ils contiennent déjà un exemplaire.", "Ranger dans les coffres proches");
            ReadFullDescription = Bind(config, section, "DescriptionComplete", KeyCode.Keypad0,
                "Relit la description complète du dernier objet annoncé (utile quand le mode bref est actif).", "Lire la description complète");

            // ---- Curseur de case libre (Cursor/FreeTileCursor.cs) ----
            // Le DÉPLACEMENT se fait aux flèches directionnelles, sans réglage : elles ne sont
            // liées à aucune action du jeu (déplacement en ZQSD/WASD) et ne servent au mod que
            // dans les menus — le curseur ne les capte donc que hors menu.
            FreeCursorToggle = Bind(config, section, "CurseurLibre", KeyCode.KeypadPeriod,
                "Active ou désactive le curseur de case libre, déplaçable aux flèches partout sur la carte.", "Activer/désactiver le curseur libre");
            FreeCursorRecenter = Bind(config, section, "CurseurLibreRecentrer", KeyCode.KeypadMultiply,
                "Ramène le curseur libre sur la case où vous vous tenez.", "Curseur libre : recentrer sur soi");

            // ---- Placement de meubles et bâtiments (Info/PlacementAssistant.cs) ----
            // La validité de l'emplacement ne s'annonce automatiquement qu'à chaque bascule ;
            // cette touche la redemande, ainsi que l'objet et son emprise au sol.
            PlacementStatus = Bind(config, section, "EtatPlacement", KeyCode.KeypadPlus,
                "Redit où en est le placement en cours : objet, emprise et validité de l'emplacement visé.", "Placement : état courant");

            // ---- Animaux de la ferme (Info/AnimalAnnouncer.cs) ----
            HerdStatus = Bind(config, section, "EtatTroupeau", KeyCode.KeypadMinus,
                "Bilan des animaux présents : combien sont à nourrir, à caresser, et combien ont laissé un produit au sol.", "Animaux : bilan du troupeau");

            // ---- Paquets à compléter : musée, autel, aquarium (Info/BundleReader.cs) ----
            BundleStatus = Bind(config, section, "EtatPaquet", KeyCode.KeypadDivide,
                "Paquet ouvert : dit ce qu'il manque encore, objet par objet, avec les quantités déjà déposées.", "Paquet : ce qu'il manque");

            // ---- Panneaux d'affichage des villes (Info/BulletinBoardReader.cs) ----
            BulletinBoardTasks = Bind(config, section, "PanneauAffichage", KeyCode.KeypadEnter,
                "Près d'un panneau d'affichage : annonce les tâches du jour, si elles sont déjà acceptées, et leur récompense.", "Panneau d'affichage : tâches du jour");

            // ---- Sorts équipés (Info/SpellAnnouncer.cs) ----
            AnnounceSpells = Bind(config, section, "SortsEquipes", KeyCode.Keypad9,
                "Annonce les quatre sorts actuellement équipés, emplacement par emplacement.", "Sorts équipés");
            EquipSpell = Bind(config, section, "ChoisirSort", KeyCode.Keypad7,
                "Ouvre au clavier le choix de sort d'un emplacement. Chaque appui passe à l'emplacement suivant, puis referme.", "Choisir un sort à équiper");
        }

        private static ConfigEntry<KeyCode> Bind(ConfigFile config, string section, string key,
            KeyCode defaultValue, string description, string label)
        {
            ConfigEntry<KeyCode> entry = config.Bind(section, key, defaultValue, description);
            All.Add((label, entry));
            return entry;
        }
    }
}
