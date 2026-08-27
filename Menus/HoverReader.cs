using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;
using SunHavenAccess.Dialogue;
using SunHavenAccess.Navigation;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Annonce l'élément interactif (Selectable) actuellement survolé par la souris, comme le
    /// mode "lire l'objet sous la souris" de NVDA/JAWS. Le survol standard d'Unity ne modifie
    /// pas EventSystem.currentSelectedGameObject (seul le clic le fait, et Sun Haven ne le fait
    /// même pas au clic pour la plupart de ses menus) : on fait donc notre propre raycast UI
    /// sous le curseur à chaque frame.
    ///
    /// Anti-rebond : un clic (gauche ou droit) déclenche souvent une cascade de changements
    /// d'interface transitoires (tooltip qui apparaît/disparaît, sous-menu...) qui, sans garde-
    /// fou, faisaient annoncer plusieurs choses parasites d'affilée. On attend une courte
    /// stabilisation avant d'annoncer, et on marque une pause juste après un clic.
    /// </summary>
    public static class HoverReader
    {
        private static readonly List<RaycastResult> _results = new List<RaycastResult>();

        private static UnityEngine.UI.Selectable _pendingHover;
        private static float _pendingSince;
        private static GameObject _lastAnnounced;
        private static float _suppressUntil;

        private const float DebounceSeconds = 0.12f;
        private const float ClickSuppressSeconds = 0.15f;

        public static void Tick()
        {
            // Idem : rien ne doit se superposer à une liste vocale ouverte.
            if (VoiceMenus.AnyOpen) return;

            if (EventSystem.current == null) return;
            if (DialogueReader.DialogueOnGoing) return; // ne pas parasiter la lecture d'un dialogue en cours
            if (MouseCursor.Enabled) return; // la souris directionnelle pilote le curseur pour interagir avec le monde, pas pour survoler des menus

            // Une infobulle native affichée (Wish.Tooltip) signifie qu'un objet réel est survolé :
            // on laisse TooltipReader l'annoncer (nom + quantité en une seule phrase, voir
            // Menus/TooltipReader.cs) plutôt que de risquer une annonce concurrente qui la coupe.
            if (Wish.Tooltip.Instance != null && Wish.Tooltip.Instance.gameObject.activeSelf) return;

            if (UnityEngine.Input.GetMouseButtonDown(0) || UnityEngine.Input.GetMouseButtonDown(1))
            {
                _suppressUntil = Time.unscaledTime + ClickSuppressSeconds;
            }

            UnityEngine.UI.Selectable hovered = FindHoveredSelectable();
            if (hovered != _pendingHover)
            {
                _pendingHover = hovered;
                _pendingSince = Time.unscaledTime;
            }

            if (Time.unscaledTime < _suppressUntil) return;
            if (Time.unscaledTime - _pendingSince < DebounceSeconds) return;

            GameObject go = hovered != null ? hovered.gameObject : null;
            if (go == _lastAnnounced) return;
            _lastAnnounced = go;
            if (go == null) return;

            string text = UiTextExtractor.ExtractAll(go);
            if (!string.IsNullOrWhiteSpace(text))
            {
                TolkSpeech.Speak(text, interrupt: true);
            }
        }

        private static UnityEngine.UI.Selectable FindHoveredSelectable()
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = UnityEngine.Input.mousePosition
            };
            _results.Clear();
            EventSystem.current.RaycastAll(pointerData, _results);

            foreach (RaycastResult result in _results)
            {
                UnityEngine.UI.Selectable sel = result.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
                if (sel != null && sel.interactable && sel.gameObject.activeInHierarchy)
                {
                    return sel;
                }
            }
            return null;
        }
    }
}
