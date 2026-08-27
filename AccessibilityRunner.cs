using System;
using System.Collections.Generic;
using UnityEngine;
using SunHavenAccess.Cursor;
using SunHavenAccess.Menus;
using SunHavenAccess.Input;
using SunHavenAccess.Navigation;
using SunHavenAccess.Info;
using SunHavenAccess.Patches;

namespace SunHavenAccess
{
    /// <summary>
    /// Fait tourner toute la logique d'accessibilité chaque frame. Plutôt qu'un MonoBehaviour
    /// séparé (dont Update() ne se déclenchait jamais : BepInEx instancie les plugins si tôt
    /// que la boucle de mise à jour d'Unity n'est pas encore pleinement opérationnelle pour de
    /// nouveaux composants — Awake()/OnEnable() s'exécutent, mais jamais Start()/Update()
    /// ensuite, vérifié empiriquement), ce Tick() est appelé en Postfix Harmony sur DEUX
    /// méthodes différentes à la fois (EventSystem.Update ET DOTweenComponent.Update, voir
    /// Patches/TickDriverPatch.cs) : chacune tourne réellement une fois par frame, donc sans
    /// protection, Tick() s'exécutait deux fois par frame — ce qui, pour toute action de type
    /// bascule (ex. la souris directionnelle), l'activait puis la désactivait aussitôt dans la
    /// même frame. On ne traite donc qu'un seul appel par frame réelle.
    /// </summary>
    public static class AccessibilityRunner
    {
        private static bool _loggedAlive;
        private static int _lastFrame = -1;

        public static void Tick()
        {
            if (Time.frameCount == _lastFrame) return; // déjà traité cette frame par l'autre point d'accroche
            _lastFrame = Time.frameCount;

            if (!_loggedAlive)
            {
                _loggedAlive = true;
                Plugin.Log.LogInfo("AccessibilityRunner.Tick : boucle active.");
            }

            SafeTick("ChatKeyRebinder", ChatKeyRebinder.Tick);
            SafeTick("HotkeyManager", HotkeyManager.Tick);
            SafeTick("TextInputReader", TextInputReader.Tick);
            SafeTick("FocusReader", FocusReader.Tick);
            SafeTick("HoverReader", HoverReader.Tick);
            SafeTick("TooltipReader", TooltipReader.Tick);
            SafeTick("TileCursor", TileCursor.Tick);
            SafeTick("MouseCursor", MouseCursor.Tick);
            SafeTick("HandItemAnnouncer", HandItemAnnouncer.Tick);
            SafeTick("FishingToneDriver", FishingToneDriver.Tick);
            SafeTick("PlacementAssistant", PlacementAssistant.Tick);
            SafeTick("ManaWarner", ManaWarner.Tick);
            SafeTick("MountAnnouncer", MountAnnouncer.Tick);
            SafeTick("CharacterCreationWizard", Menus.CharacterCreationWizard.Tick);
            SafeTick("CharacterCreationGuide", CharacterCreationGuide.Tick);
            SafeTick("MainMenuFocus", Menus.MainMenuFocus.Tick);
            SafeTick("TabListDriver", Menus.TabListDriver.Tick);
            SafeTick("SaveMenu", Menus.SaveMenu.Tick);
            SafeTick("PartyAnnouncer", PartyAnnouncer.Tick);
        }

        /// <summary>
        /// Modules dont l'erreur a déjà été journalisée. Ces Tick tournent SOIXANTE FOIS PAR
        /// SECONDE : un module qui échoue durablement — un champ du jeu renommé par une mise à
        /// jour — écrivait sa trace d'appel complète à chaque image. En une session, le journal
        /// atteint plusieurs gigaoctets et l'écriture disque finit par faire ramer le jeu lui-même,
        /// pour une erreur déjà entièrement décrite dès la première ligne.
        /// </summary>
        private static readonly HashSet<string> _reported = new HashSet<string>();

        private static void SafeTick(string name, Action tick)
        {
            try
            {
                tick();
            }
            catch (Exception e)
            {
                // Une seule fois par module et par session : la première trace dit tout, les
                // suivantes ne font que remplir le disque.
                if (_reported.Add(name))
                {
                    Plugin.Log.LogWarning(
                        $"Erreur dans {name} : {e}\n" +
                        $"Ce module est désormais silencieux pour le reste de la session ; il continue d'être appelé.");
                }
            }
        }
    }
}
