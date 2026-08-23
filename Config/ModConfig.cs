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

        /// <summary>Libellé + entrée, pour le menu des raccourcis (parcourable/modifiable en jeu).</summary>
        public static readonly List<(string Label, ConfigEntry<KeyCode> Entry)> All =
            new List<(string, ConfigEntry<KeyCode>)>();

        public static void Init(ConfigFile config)
        {
            const string section = "Touches";

            Help = Bind(config, section, "Aide", KeyCode.F2,
                "Rappelle la liste des touches du mod.", "Aide");
            DescribeFront = Bind(config, section, "CaseDevantVous", KeyCode.F3,
                "Décrit la case devant vous (terrain, objet, PNJ...).", "Décrire la case devant vous");
            Position = Bind(config, section, "Position", KeyCode.F4,
                "Annonce votre position et la direction regardée.", "Annoncer votre position");
            NextNpc = Bind(config, section, "PersonnageProche", KeyCode.F6,
                "Passe au personnage proche suivant et l'annonce (nom, direction, distance).", "Personnage proche suivant");
            Repeat = Bind(config, section, "Repeter", KeyCode.F7,
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
            // Renommé (ClicGauche -> ClicGaucheMonde) pour forcer la prise en compte du nouveau
            // défaut Espace : Entrée entrait en conflit avec la validation de menu (voir
            // MenuNavigator.Activate) et ^ (Caret) était peu pratique/pas intuitif.
            SimulateLeftClick = Bind(config, section, "ClicGaucheMonde", KeyCode.Space,
                "Simule un clic gauche à la position actuelle de la souris.", "Simuler un clic gauche (monde)");
            SimulateRightClick = Bind(config, section, "ClicDroit", KeyCode.Colon,
                "Simule un clic droit (avec Ctrl enfoncé ; Slash fonctionne aussi en alternative sur QWERTY).", "Simuler un clic droit (monde, avec Ctrl)");
            ShortcutsMenuToggle = Bind(config, section, "MenuRaccourcis", KeyCode.F12,
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
            ChatOpenKey = Bind(config, section, "OuvertureTchat", KeyCode.T,
                "Touche pour ouvrir le tchat/console du jeu (remplace Entrée, qui entrait en conflit avec la validation de menu).", "Ouvrir le tchat / la console du jeu");

            Clock = Bind(config, section, "Horloge", KeyCode.F5,
                "Annonce l'heure, le jour, la saison, l'année et la météo actuelle.", "Annoncer l'heure, le jour, la saison et la météo");

            Status = Bind(config, section, "Statut", KeyCode.F8,
                "Annonce la santé et le mana actuels.", "Annoncer la santé et le mana");
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
