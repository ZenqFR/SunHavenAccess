using System;
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
            SafeTick("FocusReader", FocusReader.Tick);
            SafeTick("HoverReader", HoverReader.Tick);
            SafeTick("TooltipReader", TooltipReader.Tick);
            SafeTick("TileCursor", TileCursor.Tick);
            SafeTick("MouseCursor", MouseCursor.Tick);
            SafeTick("HandItemAnnouncer", HandItemAnnouncer.Tick);
            SafeTick("FishingToneDriver", FishingToneDriver.Tick);
        }

        private static void SafeTick(string name, Action tick)
        {
            try
            {
                tick();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Erreur dans {name} : {e}");
            }
        }
    }
}
