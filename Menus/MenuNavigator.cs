using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Beaucoup de menus de Sun Haven (menu principal, sélection de personnage, options...) ne
    /// passent JAMAIS par le système de sélection d'UnityEngine.EventSystems : ils sont pilotés
    /// uniquement à la souris (Button.onClick), sans jamais appeler SetSelectedGameObject.
    /// FocusReader (qui surveille EventSystem.currentSelectedGameObject) ne peut donc rien
    /// annoncer sur ces écrans-là puisque rien n'y est jamais "sélectionné" au sens Unity.
    ///
    /// Ce navigateur construit sa propre liste des éléments interactifs actuellement visibles
    /// (tous les Selectable actifs de la scène) et permet de les parcourir/activer avec des
    /// touches dédiées, entièrement indépendantes du système d'input du jeu. Il complète
    /// FocusReader plutôt que de le remplacer : les écrans qui utilisent bien la sélection
    /// Unity (ex. choix de dialogue) continuent d'être couverts par FocusReader.
    /// </summary>
    public static class MenuNavigator
    {
        private static readonly List<Selectable> _items = new List<Selectable>();
        private static int _index = -1;

        public static void Next() => Move(1);
        public static void Previous() => Move(-1);

        private static void Move(int direction)
        {
            // Bug corrigé : Rescan() remettait _index à -1 avant même qu'on applique la
            // direction, donc "suivant" retombait TOUJOURS sur l'élément 0 et "précédent"
            // TOUJOURS sur l'avant-dernier, quel que soit le nombre de pressions déjà faites —
            // impossible de vraiment parcourir la liste (symptôme : les flèches "ne marchent
            // pas", toujours le même élément annoncé). On retrouve d'abord l'élément
            // actuellement sélectionné DANS la nouvelle liste rescannée pour partir de sa
            // position réelle, pas d'un index remis à zéro.
            Selectable previousSelection = (_index >= 0 && _index < _items.Count) ? _items[_index] : null;
            Rescan();
            if (_items.Count == 0)
            {
                TolkSpeech.Speak("Aucun élément de menu détecté à l'écran.", true);
                return;
            }
            int baseIndex = previousSelection != null ? _items.IndexOf(previousSelection) : -1;
            _index = ((baseIndex + direction) % _items.Count + _items.Count) % _items.Count;
            Announce(_items[_index]);
        }

        /// <summary>
        /// Bug corrigé : sur une pression "à froid" (aucune navigation aux flèches depuis
        /// l'apparition de l'écran actuel), cette méthode sélectionnait silencieusement le
        /// PREMIER élément interactif trouvé sur tout l'écran — y compris des éléments du HUD
        /// permanent (barre d'action, suivi de quête...) toujours présents même en jeu normal,
        /// hors de tout menu. Une pression d'Entrée involontaire pendant les déplacements pouvait
        /// donc, en deux appuis, activer un bouton du HUD au hasard (ex. ouvrir le menu Tab).
        /// Corrigé pour EXIGER une sélection déjà faite aux flèches avant de pouvoir valider —
        /// Entrée seule, même répétée, ne clique donc plus jamais rien à l'aveugle.
        /// </summary>
        public static void Activate()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                TolkSpeech.Speak("Utilisez les flèches pour sélectionner un élément avant de valider.", true);
                return;
            }

            Selectable sel = _items[_index];
            if (sel == null || !sel.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("Cet élément n'est plus disponible, nouvelle recherche.", true);
                Rescan();
                return;
            }

            if (sel is Button button)
            {
                button.onClick.Invoke();
                TolkSpeech.Speak("Activé.", true);
            }
            else if (sel is Toggle toggle)
            {
                toggle.isOn = !toggle.isOn;
                TolkSpeech.Speak(toggle.isOn ? "Coché." : "Décoché.", true);
            }
            else
            {
                // Repli générique pour les autres Selectable (sliders, éléments personnalisés
                // avec EventTrigger...) : on simule un clic pointeur.
                ExecuteEvents.Execute(sel.gameObject, new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
                TolkSpeech.Speak("Activé.", true);
            }
        }

        /// <summary>Action secondaire (Ctrl+Entrée) : équivalent d'un clic droit sur l'élément annoncé.</summary>
        public static void SecondaryActivate()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                TolkSpeech.Speak("Rien à activer.", true);
                return;
            }

            Selectable sel = _items[_index];
            if (sel == null || !sel.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("Cet élément n'est plus disponible, nouvelle recherche.", true);
                Rescan();
                return;
            }

            PointerEventData rightClick = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Right
            };
            ExecuteEvents.Execute(sel.gameObject, rightClick, ExecuteEvents.pointerClickHandler);
            TolkSpeech.Speak("Clic droit.", true);
        }

        public static void AnnounceCurrent()
        {
            if (_index < 0 || _index >= _items.Count)
            {
                Move(1);
                return;
            }
            Announce(_items[_index]);
        }

        private static void Rescan()
        {
            _items.Clear();
            // Tous les Selectable actifs et interactifs de la scène, triés haut -> bas puis
            // gauche -> droite pour un ordre de lecture naturel.
            Selectable[] all = Object.FindObjectsOfType<Selectable>();
            _items.AddRange(all
                .Where(s => s != null && s.interactable && s.gameObject.activeInHierarchy && IsVisible(s))
                .OrderByDescending(s => s.transform.position.y)
                .ThenBy(s => s.transform.position.x));
            _index = -1;
        }

        private static bool IsVisible(Selectable s)
        {
            CanvasGroup group = s.GetComponentInParent<CanvasGroup>();
            return group == null || group.alpha > 0.01f;
        }

        private static void Announce(Selectable sel)
        {
            string text = UiTextExtractor.ExtractAll(sel.gameObject);
            string suffix = "";
            if (sel is Toggle t) suffix = t.isOn ? ", coché" : ", non coché";
            TolkSpeech.Speak($"{text}{suffix}. Élément {_index + 1} sur {_items.Count}.", true);
        }
    }
}
