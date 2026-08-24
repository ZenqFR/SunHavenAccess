using UnityEngine;
using UnityEngine.EventSystems;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Lecture générique des menus : Sun Haven pilote toute sa navigation clavier/manette via
    /// UnityEngine.EventSystems.EventSystem (comme les choix de dialogue, les slots
    /// d'inventaire, les boutons de boutique...). Plutôt que de patcher chaque menu un par un,
    /// on surveille le GameObject actuellement sélectionné et on lit tout son texte à chaque
    /// changement : ça couvre inventaire, artisanat, boutiques, options, choix de dialogue,
    /// etc. de façon uniforme.
    /// </summary>
    public static class FocusReader
    {
        private static GameObject _lastFocused;
        private static string _pendingPrefix;

        /// <summary>
        /// Contexte à annoncer AVANT le prochain élément sélectionné, en une seule phrase
        /// ("Équipement, emplacement d'armure, chapeau, vide"). Utilisé par ZoneNavigator quand on
        /// change de zone : sans ça, il faudrait deux annonces concurrentes (le nom de la zone
        /// puis l'élément), dont la seconde couperait la première, puisque toute annonce de
        /// sélection interrompt la précédente.
        /// </summary>
        public static void SetPendingPrefix(string prefix) => _pendingPrefix = prefix;

        public static void Tick()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;

            GameObject current = es.currentSelectedGameObject;
            if (current == _lastFocused) return;
            _lastFocused = current;

            string prefix = _pendingPrefix;
            _pendingPrefix = null;

            if (current == null || !current.activeInHierarchy) return;

            string text = UiTextExtractor.ExtractAll(current);
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(prefix)) return;

            string spoken = string.IsNullOrWhiteSpace(prefix) ? text : $"{prefix}, {text}";
            TolkSpeech.Speak(spoken, interrupt: true);
        }
    }
}
