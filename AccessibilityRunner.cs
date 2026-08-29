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

            for (int i = 0; i < Modules.Length; i++)
            {
                SafeTick(i);
            }

            Profiler.EndFrame();
        }

        /// <summary>
        /// Les modules et leurs délégués, construits UNE FOIS au chargement.
        ///
        /// La liste s'écrivait auparavant en dix-neuf appels `SafeTick("X", X.Tick)`. Écrire un nom
        /// de méthode là où un délégué est attendu en construit un NEUF à chaque fois : dix-neuf
        /// petites allocations par image, mille cent quarante par seconde, indéfiniment, pour
        /// toujours appeler exactement les mêmes fonctions. Rien de tout cela n'était visible à la
        /// lecture — l'allocation est implicite — mais le ramasse-miettes finit par passer, et un
        /// passage de ramasse-miettes, ça se sent comme une saccade.
        ///
        /// Les noms servent aussi de clés au journal d'erreurs et à la mesure de temps ; les garder
        /// côte à côte évite qu'ils divergent.
        /// </summary>
        private static readonly (string Name, Action Tick)[] Modules =
        {
            ("ChatKeyRebinder", ChatKeyRebinder.Tick),
            ("HotkeyManager", HotkeyManager.Tick),
            ("TextInputReader", TextInputReader.Tick),
            ("FocusReader", FocusReader.Tick),
            ("HoverReader", HoverReader.Tick),
            ("TooltipReader", TooltipReader.Tick),
            ("TileCursor", TileCursor.Tick),
            ("MouseCursor", MouseCursor.Tick),
            ("HandItemAnnouncer", HandItemAnnouncer.Tick),
            ("FishingToneDriver", FishingToneDriver.Tick),
            ("PlacementAssistant", PlacementAssistant.Tick),
            ("ManaWarner", ManaWarner.Tick),
            ("MountAnnouncer", MountAnnouncer.Tick),
            ("CharacterCreationWizard", Menus.CharacterCreationWizard.Tick),
            ("CharacterCreationGuide", CharacterCreationGuide.Tick),
            ("MainMenuFocus", Menus.MainMenuFocus.Tick),
            ("TabListDriver", Menus.TabListDriver.Tick),
            ("SaveMenu", Menus.SaveMenu.Tick),
            ("PartyAnnouncer", PartyAnnouncer.Tick),
            ("Journey", Navigation.Journey.Tick),
            ("KeyConflictCheck", KeyConflictCheck.RunOnce),
            ("DialogueChoiceMenu", Dialogue.DialogueChoiceMenu.Tick),
            ("StepMovement", StepMovement.Tick),
        };

        /// <summary>
        /// Modules dont l'erreur a déjà été journalisée. Ces Tick tournent SOIXANTE FOIS PAR
        /// SECONDE : un module qui échoue durablement — un champ du jeu renommé par une mise à
        /// jour — écrivait sa trace d'appel complète à chaque image. En une session, le journal
        /// atteint plusieurs gigaoctets et l'écriture disque finit par faire ramer le jeu lui-même,
        /// pour une erreur déjà entièrement décrite dès la première ligne.
        /// </summary>
        private static readonly HashSet<string> _reported = new HashSet<string>();

        private static void SafeTick(int index)
        {
            long started = Profiler.Enabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;

            try
            {
                Modules[index].Tick();
            }
            catch (Exception e)
            {
                // Une seule fois par module et par session : la première trace dit tout, les
                // suivantes ne font que remplir le disque.
                if (_reported.Add(Modules[index].Name))
                {
                    Plugin.Log.LogWarning(
                        $"Erreur dans {Modules[index].Name} : {e}\n" +
                        $"Ce module est désormais silencieux pour le reste de la session ; il continue d'être appelé.");
                }
            }

            if (Profiler.Enabled) Profiler.Record(index, System.Diagnostics.Stopwatch.GetTimestamp() - started);
        }

        /// <summary>
        /// Mesure du temps passé par module, pour corriger sur des chiffres plutôt que sur des
        /// intuitions.
        ///
        /// Chercher une lenteur en devinant fait perdre des heures et mène souvent à optimiser ce
        /// qui ne coûtait rien. Le mod se chronomètre donc lui-même et n'écrit dans le journal QUE
        /// lorsqu'un module dépasse un seuil visible à l'œil — silence complet quand tout va bien,
        /// nom du coupable et temps moyen sinon.
        ///
        /// La mesure elle-même est deux lectures d'horloge par module et par image, sans
        /// allocation : moins cher que ce qu'elle permet de trouver. Elle se coupe entièrement
        /// depuis le fichier de configuration pour qui n'en veut pas.
        /// </summary>
        internal static class Profiler
        {
            /// <summary>Fenêtre d'observation. Assez longue pour lisser, assez courte pour réagir.</summary>
            private const float WindowSeconds = 10f;

            /// <summary>
            /// Seuil de signalement, en millisecondes par image. Une image dure seize millisecondes
            /// à soixante par seconde ; un dixième de milliseconde pour TOUT le mod serait déjà
            /// généreux, donc un demi pour un seul module est franchement anormal.
            /// </summary>
            private const double ReportThresholdMs = 0.5;

            internal static bool Enabled = true;

            private static readonly long[] _ticks = new long[64];
            private static int _frames;
            private static float _nextReport;

            internal static void Record(int index, long elapsed)
            {
                if (index >= 0 && index < _ticks.Length) _ticks[index] += elapsed;
            }

            internal static void EndFrame()
            {
                if (!Enabled) return;
                _frames++;

                float now = UnityEngine.Time.unscaledTime;
                if (_nextReport == 0f) { _nextReport = now + WindowSeconds; return; }
                if (now < _nextReport) return;
                _nextReport = now + WindowSeconds;

                if (_frames > 0)
                {
                    double toMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                    for (int i = 0; i < Modules.Length; i++)
                    {
                        double perFrame = _ticks[i] * toMs / _frames;
                        if (perFrame < ReportThresholdMs) continue;
                        Plugin.Log?.LogWarning(
                            $"Performance : {Modules[i].Name} prend {perFrame:F2} ms par image " +
                            $"(mesuré sur {_frames} images).");
                    }
                }

                Array.Clear(_ticks, 0, _ticks.Length);
                _frames = 0;
            }
        }
    }
}
