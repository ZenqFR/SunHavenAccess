using UnityEngine;
using UnityEngine.EventSystems;
using SunHavenAccess.Speech;
using SunHavenAccess.Cursor;
using SunHavenAccess.Navigation;
using SunHavenAccess.Menus;
using SunHavenAccess.Localization;
using SunHavenAccess.Config;
using SunHavenAccess.Info;

namespace SunHavenAccess.Input
{
    /// <summary>
    /// Touches d'accessibilité. Chaque action utilise la touche définie dans le fichier de
    /// config du mod (voir Config/ModConfig.cs, modifiable dans
    /// BepInEx/config/com.kleitz.sunhavenaccess.cfg ou en jeu via le menu des raccourcis),
    /// vérifiée à la fois via UnityEngine.Input et via Rewired (le système d'input du jeu a sa
    /// propre lecture du clavier, séparée de celle d'Unity). Deux combinaisons Ctrl+touche
    /// (clic droit monde, action secondaire de menu) sont gérées ici directement, en dehors du
    /// système de config à touche unique.
    /// </summary>
    public static class HotkeyManager
    {
        public static void Tick()
        {
            // Le menu des raccourcis a la main exclusive tant qu'il est ouvert, pour que les
            // touches de navigation/validation ne déclenchent pas aussi les actions normales.
            if (Pressed(ModConfig.ShortcutsMenuToggle.Value))
            {
                ShortcutsMenu.Toggle();
                return;
            }
            if (ShortcutsMenu.IsOpen)
            {
                ShortcutsMenu.Tick();
                return;
            }

            if (Pressed(ModConfig.Repeat.Value)) TolkSpeech.Repeat();
            if (Pressed(ModConfig.DescribeFront.Value)) TileCursor.AnnounceFront();
            if (Pressed(ModConfig.Position.Value)) TileCursor.AnnouncePosition();
            if (Pressed(ModConfig.NextNpc.Value)) NPCFinder.AnnounceNext();
            if (Pressed(ModConfig.ToggleVerbosity.Value)) TileCursor.ToggleVerbosity();
            if (Pressed(ModConfig.Help.Value)) AnnounceHelp();
            if (Pressed(ModConfig.TurnLeft.Value)) TileCursor.Turn(-1);
            if (Pressed(ModConfig.TurnRight.Value)) TileCursor.Turn(1);
            if (Pressed(ModConfig.TestTone.Value)) TestTone.Play(Plugin.Log);
            if (Pressed(ModConfig.MouseFollowToggle.Value)) MouseCursor.Toggle();
            if (Pressed(ModConfig.Clock.Value)) ClockAnnouncer.Announce();
            if (Pressed(ModConfig.Status.Value)) StatusAnnouncer.Announce();
            if (Pressed(ModConfig.FishingToneToggle.Value)) FishingToneCue.ToggleEnabled();
            if (Pressed(ModConfig.AnnounceQuests.Value)) QuestAnnouncer.AnnounceActiveQuests();
            if (Pressed(ModConfig.AnnounceRelationships.Value)) RelationshipAnnouncer.AnnounceAll();
            if (Pressed(ModConfig.AnnounceProfessions.Value)) ProfessionAnnouncer.AnnounceAll();
            if (Pressed(ModConfig.MapPreviousLocation.Value)) MapNavigator.AnnouncePrevious();
            if (Pressed(ModConfig.MapNextLocation.Value)) MapNavigator.AnnounceNext();

            // Navigation de menu : flèches directionnelles (Haut/Gauche = précédent,
            // Bas/Droite = suivant, quelle que soit la touche exacte choisie en config, pour
            // que les deux paires fonctionnent par défaut). Ctrl+Entrée = clic droit,
            // vérifié EN PREMIER pour ne pas aussi déclencher le simple clic gauche.
            bool ctrl = CtrlHeld();

            // Apparence en création de personnage : même convention que le scanner (touche
            // seule = option, Ctrl+touche = catégorie).
            if (Pressed(ModConfig.AppearancePrevious.Value))
            {
                if (ctrl) CharacterAppearanceNavigator.PreviousCategory(); else CharacterAppearanceNavigator.PreviousOption();
            }
            if (Pressed(ModConfig.AppearanceNext.Value))
            {
                if (ctrl) CharacterAppearanceNavigator.NextCategory(); else CharacterAppearanceNavigator.NextOption();
            }
            if (Pressed(ModConfig.AnnounceSkillPoints.Value)) SkillPointsAnnouncer.AnnounceAll();
            if (Pressed(ModConfig.AnnounceFestivals.Value)) FestivalAnnouncer.AnnounceThisSeason();

            // Ctrl+Tab / Ctrl+Maj+Tab : bascule directement d'onglet dans le menu principal
            // (Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques,
            // Paramètres) sans avoir à naviguer aux flèches toute la liste d'éléments pour
            // retrouver les boutons d'onglet, souvent tout en haut de l'écran.
            if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                MenuNavigator.SwitchMajorTab(shift ? -1 : 1);
            }

            // Certains écrans (arbre de compétences, choix de dialogue...) utilisent, EUX,
            // vraiment la sélection native d'Unity (contrairement au menu principal, piloté
            // uniquement à la souris) — trouvé en décompilant Wish.SkillNode : elle implémente
            // ISelectHandler/ISubmitHandler et déclenche elle-même l'infobulle native au
            // survol/sélection (déjà lue par TooltipReader). Si un objet est RÉELLEMENT
            // sélectionné là, Rewired pilote déjà les flèches/Entrée nativement pour cet
            // écran : faire AUSSI avancer notre propre liste MenuNavigator par-dessus créerait
            // deux systèmes de navigation concurrents désynchronisés (le joueur entendrait
            // l'annonce d'un élément différent de celui réellement sélectionné, ou une
            // validation déclenchée deux fois). On laisse donc le jeu gérer nativement dans ce
            // cas — FocusReader + TooltipReader s'occupent déjà d'annoncer les changements.
            bool nativeSelectionActive = EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject != null
                && EventSystem.current.currentSelectedGameObject.activeInHierarchy;

            // Sac à dos/équipement/barre d'action (Wish.Slot/ArmorSlot) : cas plus précis que le
            // passe-plat générique ci-dessus, demande explicite de navigation directionnelle
            // réelle. Voir Menus/InventoryGridNavigator.cs — jamais testé en jeu.
            if (InventoryGridNavigator.IsActive())
            {
                if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) InventoryGridNavigator.Move(Vector2Int.up, true);
                else if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) InventoryGridNavigator.Move(Vector2Int.down, true);
                else if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) InventoryGridNavigator.Move(Vector2Int.left, true);
                else if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) InventoryGridNavigator.Move(Vector2Int.right, true);
                else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) InventoryGridNavigator.Move(Vector2Int.up, false);
                else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) InventoryGridNavigator.Move(Vector2Int.down, false);
                else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) InventoryGridNavigator.Move(Vector2Int.left, false);
                else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) InventoryGridNavigator.Move(Vector2Int.right, false);

                if (!ctrl)
                {
                    // Rangée de chiffres (même position physique en AZERTY qu'en QWERTY pour ces
                    // KeyCode) : envoie/récupère l'objet du slot actuel vers/depuis la barre
                    // d'action, index 0-9.
                    KeyCode[] digitKeys = { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
                        KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
                    for (int i = 0; i < digitKeys.Length; i++)
                    {
                        if (UnityEngine.Input.GetKeyDown(digitKeys[i]))
                        {
                            InventoryGridNavigator.QuickAssign(i);
                            break;
                        }
                    }
                }
            }
            else if (!nativeSelectionActive)
            {
                // Ctrl+Gauche/Droite ajuste un curseur (Slider) sélectionné (ex. couleurs en
                // création de personnage) — vérifié EN PREMIER pour que Ctrl+flèche n'avance/
                // recule pas AUSSI dans la liste des éléments du menu.
                if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) MenuNavigator.AdjustSlider(-1);
                else if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) MenuNavigator.AdjustSlider(1);
                else if (Pressed(ModConfig.MenuPrevious.Value) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) MenuNavigator.Previous();
                else if (Pressed(ModConfig.MenuNext.Value) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) MenuNavigator.Next();
                if (ctrl && UnityEngine.Input.GetKeyDown(ModConfig.MenuActivate.Value))
                {
                    MenuNavigator.SecondaryActivate();
                }
                else if (Pressed(ModConfig.MenuActivate.Value))
                {
                    MenuNavigator.Activate();
                }
            }

            // Clic droit dans le monde (souris directionnelle) : Ctrl + la touche configurée
            // (":" par défaut sur AZERTY) ou Ctrl+Slash ("/", même touche physique sur QWERTY).
            if (ctrl && (UnityEngine.Input.GetKeyDown(ModConfig.SimulateRightClick.Value) || UnityEngine.Input.GetKeyDown(KeyCode.Slash)))
            {
                MouseCursor.SimulateRightClick();
            }
            if (Pressed(ModConfig.SimulateLeftClick.Value)) MouseCursor.SimulateLeftClick();

            // Scanner par catégories (PNJ, plantations, ressources, bâtiments...), même
            // convention que stardew-access : Page seule = élément, Ctrl+Page = catégorie,
            // Origine seule = info sur l'élément sélectionné, Ctrl+Origine = s'y rendre à pied
            // automatiquement, Fin = nombre trouvé.
            if (Pressed(ModConfig.ScannerPageUp.Value))
            {
                if (ctrl) Scanner.PreviousCategory(); else Scanner.PreviousItem();
            }
            if (Pressed(ModConfig.ScannerPageDown.Value))
            {
                if (ctrl) Scanner.NextCategory(); else Scanner.NextItem();
            }
            if (Pressed(ModConfig.ScannerNearest.Value))
            {
                if (ctrl) Scanner.TravelToCurrent(); else Scanner.AnnounceInfo();
            }
            if (Pressed(ModConfig.ScannerCount.Value)) Scanner.AnnounceCount();

            // Échap annule un cheminement automatique en cours, où qu'on soit (ne fait rien si
            // aucun cheminement n'est en cours, donc n'interfère pas avec les autres usages
            // d'Échap ailleurs dans le jeu).
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) && PathingController.IsPathing)
            {
                PathingController.Cancel();
            }
        }

        private static bool CtrlHeld() =>
            UnityEngine.Input.GetKey(KeyCode.LeftControl) || UnityEngine.Input.GetKey(KeyCode.RightControl);

        private static bool Pressed(KeyCode key)
        {
            if (key == KeyCode.None) return false;
            return UnityEngine.Input.GetKeyDown(key) || RewiredKeyDown(key);
        }

        /// <summary>
        /// Sun Haven utilise Rewired pour tout son input clavier/manette, qui a sa PROPRE
        /// lecture du clavier, séparée de celle d'UnityEngine.Input. Si Rewired a la main
        /// exclusive dessus, Input.GetKeyDown peut ne jamais rien voir passer alors même que
        /// le jeu répond normalement aux touches. On interroge donc aussi directement le
        /// clavier de Rewired, en repli.
        /// </summary>
        private static bool RewiredKeyDown(KeyCode key)
        {
            try
            {
                if (!Rewired.ReInput.isReady) return false;
                Rewired.Keyboard kb = Rewired.ReInput.controllers.Keyboard;
                return kb != null && kb.GetKeyDown(key);
            }
            catch
            {
                return false;
            }
        }

        private static void AnnounceHelp()
        {
            TolkSpeech.Speak(
                "Touches d'accessibilité : " +
                $"{Strings.KeyName(ModConfig.DescribeFront.Value)}, décrire la case devant vous. " +
                $"{Strings.KeyName(ModConfig.Position.Value)}, votre position. " +
                $"{Strings.KeyName(ModConfig.Clock.Value)}, l'heure, le jour, la saison et la météo. " +
                $"{Strings.KeyName(ModConfig.FishingToneToggle.Value)}, activer ou désactiver le bip continu de visée pendant la pêche. " +
                $"{Strings.KeyName(ModConfig.Status.Value)}, votre santé et votre mana. " +
                $"{Strings.KeyName(ModConfig.AnnounceQuests.Value)}, annoncer vos quêtes actives. " +
                $"{Strings.KeyName(ModConfig.AnnounceRelationships.Value)}, annoncer vos relations avec les PNJ. " +
                $"{Strings.KeyName(ModConfig.AnnounceProfessions.Value)}, annoncer vos niveaux de compétence. " +
                $"{Strings.KeyName(ModConfig.MapPreviousLocation.Value)} et {Strings.KeyName(ModConfig.MapNextLocation.Value)}, lieu précédent ou suivant sur la carte du monde, carte ouverte. " +
                $"{Strings.KeyName(ModConfig.AppearancePrevious.Value)} et {Strings.KeyName(ModConfig.AppearanceNext.Value)}, option précédente ou suivante d'apparence en création de personnage. Contrôle plus l'une ou l'autre, catégorie précédente ou suivante. " +
                $"{Strings.KeyName(ModConfig.AnnounceSkillPoints.Value)}, annoncer les points de compétence disponibles dans chaque arbre. " +
                $"{Strings.KeyName(ModConfig.AnnounceFestivals.Value)}, annoncer les festivals de la saison actuelle. " +
                $"{Strings.KeyName(ModConfig.NextNpc.Value)}, personnage proche suivant. " +
                $"{Strings.KeyName(ModConfig.Repeat.Value)}, répéter. " +
                $"{Strings.KeyName(ModConfig.ToggleVerbosity.Value)}, activer ou désactiver l'annonce automatique des déplacements. " +
                $"{Strings.KeyName(ModConfig.TurnLeft.Value)} et {Strings.KeyName(ModConfig.TurnRight.Value)}, tourner sans vous déplacer. " +
                $"{Strings.KeyName(ModConfig.MouseFollowToggle.Value)}, activer ou désactiver la souris qui pointe vers la case devant vous. " +
                $"{Strings.KeyName(ModConfig.SimulateLeftClick.Value)}, clic gauche. Contrôle plus {Strings.KeyName(ModConfig.SimulateRightClick.Value)}, clic droit. " +
                "Dans les menus : flèches directionnelles pour parcourir, Entrée pour un clic gauche, Contrôle plus Entrée pour un clic droit, " +
                "Contrôle plus flèche gauche ou droite pour ajuster un curseur sélectionné, Contrôle plus Tabulation ou Contrôle plus Majuscule plus Tabulation " +
                "pour changer d'onglet directement dans le menu principal. " +
                "Scanner : Page précédente et Page suivante pour parcourir les éléments trouvés, Contrôle plus Page précédente ou suivante pour changer de catégorie " +
                "parmi personnages, plantations, ressources, bâtiments et portails, animaux et compagnons, ennemis, mobilier et rangement. " +
                "Origine pour annoncer l'élément sélectionné, Contrôle plus Origine pour vous y rendre automatiquement, Échap pour annuler le trajet, Fin pour connaître le nombre trouvé. " +
                $"{Strings.KeyName(ModConfig.ChatOpenKey.Value)}, ouvrir le tchat ou la console du jeu (remplace Entrée, qui entrait en conflit avec la validation de menu). " +
                $"{Strings.KeyName(ModConfig.Help.Value)}, cette aide. " +
                $"{Strings.KeyName(ModConfig.ShortcutsMenuToggle.Value)}, ouvre le menu complet des raccourcis, qui permet aussi de changer chaque touche.", true);
        }
    }
}
