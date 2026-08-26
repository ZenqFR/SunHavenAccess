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

        /// <summary>
        /// Ignore UNE seule annonce de changement de sélection. Utilisé quand un autre système du
        /// mod annonce déjà mieux ce même élément : typiquement la sélection d'une icône d'objet,
        /// dont TooltipReader lit le nom, la description ET la quantité — laisser les deux parler
        /// donnerait deux annonces concurrentes, la première coupée en plein mot.
        /// </summary>
        public static void SuppressNextAnnouncement() => _suppressNext = true;

        private static bool _suppressNext;

        public static void Tick()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;

            GameObject current = es.currentSelectedGameObject;

            // Une liste vocale ouverte par-dessus un menu du jeu est la SEULE chose qui doit
            // parler. Rendre les flèches inertes pour le jeu ne suffit pas : Sun Haven lit le
            // clavier par Rewired, indépendamment du système d'évènements d'Unity, donc sa
            // sélection peut continuer de bouger sous la liste. On annonçait alors les deux à la
            // fois, chacun coupant l'autre — ce qui donnait l'impression que la liste ne changeait
            // rien. On suit la sélection sans rien en dire, pour ne pas annoncer un déplacement
            // périmé au moment où la liste se referme.
            if (ListMenu.IsOpen || HelpMenu.IsOpen || ShortcutsMenu.IsOpen)
            {
                _lastFocused = current;
                _pendingPrefix = null;
                return;
            }

            if (current == _lastFocused) return;
            _lastFocused = current;

            string prefix = _pendingPrefix;
            _pendingPrefix = null;

            bool suppressed = _suppressNext;
            _suppressNext = false;
            if (suppressed) return;

            if (current == null || !current.activeInHierarchy) return;

            string text = UiTextExtractor.ExtractAll(current);
            if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(prefix)) return;

            string spoken = string.IsNullOrWhiteSpace(prefix) ? text : $"{prefix}, {text}";
            TolkSpeech.Speak(spoken, interrupt: true);
        }
    }
}
