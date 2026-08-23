using UnityEngine;
using SunHavenAccess.Speech;
using SunHavenAccess.Localization;
using SunHavenAccess.Config;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Menu vocal listant tous les raccourcis du mod, leur touche actuelle, ce qu'ils font, et
    /// permettant de changer la touche directement en jeu (le changement est aussitôt écrit
    /// dans le fichier de config du mod par BepInEx). Entièrement piloté par le clavier, sans
    /// interface visuelle nécessaire.
    /// </summary>
    public static class ShortcutsMenu
    {
        private static bool _open;
        private static int _index;
        private static bool _awaitingKey;

        public static bool IsOpen => _open;

        public static void Toggle()
        {
            _open = !_open;
            _awaitingKey = false;
            if (_open)
            {
                _index = 0;
                TolkSpeech.Speak(
                    "Menu des raccourcis ouvert. Utilisez les touches de navigation de menu pour " +
                    "parcourir la liste, la touche de validation pour changer une touche, et " +
                    "rouvrez ce menu pour le fermer.", true);
                AnnounceCurrent();
            }
            else
            {
                TolkSpeech.Speak("Menu des raccourcis fermé.", true);
            }
        }

        /// <summary>
        /// À appeler chaque frame tant que le menu est ouvert : gère entièrement la navigation,
        /// la validation et la capture de la nouvelle touche en mode réaffectation. Tant que ce
        /// menu est ouvert, il a la main exclusive (voir HotkeyManager).
        /// </summary>
        public static void Tick()
        {
            if (!_open) return;

            if (_awaitingKey)
            {
                CaptureNewKey();
                return;
            }

            if (UnityEngine.Input.GetKeyDown(ModConfig.MenuPrevious.Value) || UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) MovePrevious();
            else if (UnityEngine.Input.GetKeyDown(ModConfig.MenuNext.Value) || UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) MoveNext();
            else if (UnityEngine.Input.GetKeyDown(ModConfig.MenuActivate.Value)) BeginRebind();
        }

        private static void MoveNext()
        {
            _index = (_index + 1) % ModConfig.All.Count;
            AnnounceCurrent();
        }

        private static void MovePrevious()
        {
            _index = (_index - 1 + ModConfig.All.Count) % ModConfig.All.Count;
            AnnounceCurrent();
        }

        private static void BeginRebind()
        {
            _awaitingKey = true;
            (string label, var entry) = ModConfig.All[_index];
            TolkSpeech.Speak($"Appuyez sur la nouvelle touche pour {label}, ou sur Échap pour annuler.", true);
        }

        private static void CaptureNewKey()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                _awaitingKey = false;
                TolkSpeech.Speak("Changement annulé.", true);
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || key == KeyCode.Escape) continue;
                if (!UnityEngine.Input.GetKeyDown(key)) continue;

                (string label, var entry) = ModConfig.All[_index];
                entry.Value = key; // BepInEx sauvegarde automatiquement le fichier de config
                _awaitingKey = false;
                TolkSpeech.Speak($"Touche pour {label} changée en {Strings.KeyName(key)}.", true);
                return;
            }
        }

        private static void AnnounceCurrent()
        {
            (string label, var entry) = ModConfig.All[_index];
            TolkSpeech.Speak(
                $"{label} : touche {Strings.KeyName(entry.Value)}. {entry.Description.Description} " +
                $"Élément {_index + 1} sur {ModConfig.All.Count}.", true);
        }
    }
}
