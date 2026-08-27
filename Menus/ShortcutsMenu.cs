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
                TolkSpeech.Speak(Localization.Language.T(
                    "Menu des raccourcis ouvert. Utilisez les touches de navigation de menu pour " +
                    "parcourir la liste, la touche de validation pour changer une touche, et " +
                    "rouvrez ce menu pour le fermer.",
                    "Shortcuts menu open. Use the menu navigation keys to browse the list, the " +
                    "confirm key to change a key, and reopen this menu to close it."), true);
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
            string name = Localization.Translator.Translate(label);
            TolkSpeech.Speak(Localization.Language.T(
                $"Appuyez sur la nouvelle touche pour {name}. " +
                "Retour arrière ou Suppression pour n'assigner aucune touche, Échap pour annuler.",
                $"Press the new key for {name}. " +
                "Backspace or Delete to bind no key at all, Escape to cancel."), true);
        }

        private static void CaptureNewKey()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                _awaitingKey = false;
                TolkSpeech.Speak("Changement annulé.", true);
                return;
            }

            // Retirer l'assignation. Sans ça, un raccourci ne pouvait que se déplacer d'une touche
            // à une autre, jamais disparaître : une action dont on ne veut pas continuait d'occuper
            // une touche du clavier, et il fallait éditer le fichier de config à la main.
            if (UnityEngine.Input.GetKeyDown(KeyCode.Backspace) || UnityEngine.Input.GetKeyDown(KeyCode.Delete))
            {
                (string cleared, var clearedEntry) = ModConfig.All[_index];
                clearedEntry.Value = KeyCode.None;
                _awaitingKey = false;
                TolkSpeech.Speak(Localization.Language.T(
                    $"{Localization.Translator.Translate(cleared)} n'est plus assigné à aucune touche.",
                    $"{Localization.Translator.Translate(cleared)} is no longer bound to any key."), true);
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || key == KeyCode.Escape) continue;
                if (key == KeyCode.Backspace || key == KeyCode.Delete) continue; // réservées au retrait
                if (!UnityEngine.Input.GetKeyDown(key)) continue;

                (string label, var entry) = ModConfig.All[_index];
                entry.Value = key; // BepInEx sauvegarde automatiquement le fichier de config
                _awaitingKey = false;
                TolkSpeech.Speak(Localization.Language.T(
                    $"Touche pour {Localization.Translator.Translate(label)} changée en {Strings.KeyName(key)}.",
                    $"Key for {Localization.Translator.Translate(label)} changed to {Strings.KeyName(key)}."), true);
                return;
            }
        }

        private static void AnnounceCurrent()
        {
            (string label, var entry) = ModConfig.All[_index];

            // « touche non assignée » se dirait mal : sans touche, on annonce l'état, pas une
            // touche absente.
            string keyPart = entry.Value == KeyCode.None
                ? Localization.Language.T("non assigné", "unassigned")
                : Localization.Language.T($"touche {Strings.KeyName(entry.Value)}",
                                          $"key {Strings.KeyName(entry.Value)}");

            // Le libellé et la description sont écrits en français dans ModConfig — ils servent
            // aussi de commentaires dans le fichier de configuration, qui reste français. La table
            // de traduction les reprend ; celles qui n'y figurent pas ressortent en français
            // plutôt qu'à moitié traduites.
            string name = Localization.Translator.Translate(label);
            string description = Localization.Translator.Translate(entry.Description.Description);

            TolkSpeech.Speak(Localization.Language.T(
                $"{name} : {keyPart}. {description} Élément {_index + 1} sur {ModConfig.All.Count}.",
                $"{name}: {keyPart}. {description} Item {_index + 1} of {ModConfig.All.Count}."), true);
        }
    }
}
