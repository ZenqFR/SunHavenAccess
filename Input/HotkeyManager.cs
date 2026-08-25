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

            // Pendant la frappe dans un champ de saisie, TOUTES les touches du mod sont
            // suspendues : sinon taper « p » dans le nom de son personnage annoncerait la
            // position, « o » l'horloge, « c » ouvrirait le tchat... Voir Menus/TextInputReader.cs.
            // On rétablit d'abord la navigation native, sinon elle resterait neutralisée pour
            // toute la durée de la saisie (et au-delà, puisqu'on sort avant d'y toucher).
            if (TextInputReader.IsTyping())
            {
                SuppressNativeNavigation(false);
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
            if (Pressed(ModConfig.SortBackpack.Value)) InventoryActions.SortBackpack();
            if (Pressed(ModConfig.AnnounceContents.Value)) InventoryActions.AnnounceContents();
            if (Pressed(ModConfig.StoreInChests.Value)) InventoryActions.StoreInNearbyChests();
            if (Pressed(ModConfig.ReadFullDescription.Value)) AnnounceFullDescription();

            // Ctrl+Tab / Ctrl+Maj+Tab : bascule directement d'onglet dans le menu principal
            // (Sac à dos, Arbre de compétences, Relations, Quêtes, Carte, Statistiques,
            // Paramètres) sans avoir à naviguer aux flèches toute la liste d'éléments pour
            // retrouver les boutons d'onglet, souvent tout en haut de l'écran.
            if (ctrl && UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                bool shift = UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift);
                // wrap:true — Ctrl+Tab est un geste de CYCLE (comme dans un navigateur), il fait
                // donc le tour ; les flèches gauche/droite dans la zone Onglets butent, elles,
                // sur un bip de bord comme partout ailleurs.
                ZoneNavigator.SwitchTab(shift ? -1 : 1, wrap: true);
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

            // Navigation directionnelle du mod (Menus/ZoneNavigator.cs) : flèches selon la
            // disposition visuelle réelle, avec des zones aux frontières nettes.
            bool directionalNav = ModConfig.TakeOverMenuArrows.Value && ZoneNavigator.IsActive();
            SuppressNativeNavigation(directionalNav);

            if (directionalNav)
            {
                HandleDirectionalNavigation(ctrl);
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

        /// <summary>
        /// Flèches en mode navigation directionnelle. Ctrl+flèche = saut vers la zone voisine,
        /// flèche seule = déplacement dans la zone courante (bip de bord si on bute).
        /// Entrée/Ctrl+Entrée sont gérés ici aussi : la validation NATIVE d'Unity est neutralisée
        /// pendant ce mode (voir SuppressNativeNavigation), c'est donc au mod de l'émettre.
        /// </summary>
        private static void HandleDirectionalNavigation(bool ctrl)
        {
            // Ctrl+gauche/droite sur un curseur (Slider) = ajuster sa valeur, PAS changer de
            // zone : c'est le seul cas où Ctrl+flèche ne veut pas dire "saut de zone", vérifié
            // en premier pour cette raison.
            bool onSlider = MenuNavigator.SelectedIsSlider();

            if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow))
            {
                if (ctrl && onSlider) MenuNavigator.AdjustSelectedSlider(-1);
                else ZoneNavigator.Move(-1, 0, ctrl);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (ctrl && onSlider) MenuNavigator.AdjustSelectedSlider(1);
                else ZoneNavigator.Move(1, 0, ctrl);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow))
            {
                ZoneNavigator.Move(0, 1, ctrl);
            }
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow))
            {
                ZoneNavigator.Move(0, -1, ctrl);
            }

            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

            // Pressed() plutôt que Input.GetKeyDown seul : le jeu lit le clavier via Rewired, qui
            // peut être le seul des deux à voir la touche (même convention que partout ailleurs).
            if (Pressed(ModConfig.MenuActivate.Value))
            {
                if (selected == null) TolkSpeech.Speak("Aucun élément sélectionné.", true);
                else if (ctrl) MenuNavigator.SecondaryActivateObject(selected);
                else MenuNavigator.ActivateObject(selected);
            }

            if (ctrl) return;

            // Rangée de chiffres (mêmes touches physiques en AZERTY qu'en QWERTY pour ces
            // KeyCode) : envoie/récupère l'objet de l'emplacement sélectionné vers/depuis le
            // slot de barre d'action correspondant.
            for (int i = 0; i < DigitKeys.Length; i++)
            {
                if (UnityEngine.Input.GetKeyDown(DigitKeys[i]))
                {
                    ZoneNavigator.QuickAssign(i);
                    break;
                }
            }
        }

        private static readonly KeyCode[] DigitKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5,
            KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0,
        };

        private static bool _navSuppressed;

        /// <summary>
        /// Neutralise la navigation clavier NATIVE d'Unity tant que le mod pilote les flèches.
        /// Indispensable : la boucle du mod tourne en Postfix sur `EventSystem.Update`, donc
        /// APRÈS le traitement d'input du jeu — sans ça, une seule pression sur une flèche
        /// déplacerait le curseur deux fois (une par le jeu, une par le mod).
        /// Conséquence assumée : la VALIDATION native (Entrée) est neutralisée elle aussi, le mod
        /// l'émet donc lui-même (voir HandleDirectionalNavigation). La fermeture des menus
        /// (Échap) n'est pas concernée : le jeu la lit directement via Rewired dans son propre
        /// Update, pas via les évènements de l'EventSystem.
        /// </summary>
        private static void SuppressNativeNavigation(bool suppress)
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;

            if (suppress && !_navSuppressed)
            {
                es.sendNavigationEvents = false;
                _navSuppressed = true;
            }
            else if (!suppress && _navSuppressed)
            {
                es.sendNavigationEvents = true;
                _navSuppressed = false;
            }
        }


        /// <summary>
        /// Relit la description complète du dernier objet annoncé. Indispensable avec le mode
        /// bref (qui n'annonce que quantité + nom en parcourant), et pratique même sans lui pour
        /// réécouter sans avoir à repasser sur la case. Utilise le texte MÉMORISÉ, donc marche
        /// aussi une fois l'infobulle refermée.
        /// </summary>
        private static void AnnounceFullDescription()
        {
            string text = TooltipReader.LastFullText;
            TolkSpeech.Speak(
                string.IsNullOrWhiteSpace(text) ? "Aucune description disponible." : text,
                true);
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
                $"Dans l'inventaire : {Strings.KeyName(ModConfig.SortBackpack.Value)}, trier le sac. " +
                $"{Strings.KeyName(ModConfig.AnnounceContents.Value)}, résumé du contenu. " +
                $"{Strings.KeyName(ModConfig.StoreInChests.Value)}, ranger dans les coffres proches. " +
                $"{Strings.KeyName(ModConfig.ReadFullDescription.Value)}, lire la description complète de l'objet annoncé. " +
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
