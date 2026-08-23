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

        public static void Tick()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;

            GameObject current = es.currentSelectedGameObject;
            if (current == _lastFocused) return;
            _lastFocused = current;

            if (current == null || !current.activeInHierarchy) return;

            string text = UiTextExtractor.ExtractAll(current);
            if (!string.IsNullOrWhiteSpace(text))
            {
                TolkSpeech.Speak(text, interrupt: true);
            }
        }
    }
}
