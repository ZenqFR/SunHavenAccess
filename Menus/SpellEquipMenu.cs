using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Ouvre au clavier le choix de sort d'un emplacement.
    ///
    /// Changer un sort équipé était jusqu'ici IMPOSSIBLE sans souris : la liste s'ouvre au survol
    /// (`Wish.SpellSelection`, `IPointerEnter`/`IPointerExit`) et rien ne l'atteint au clavier.
    ///
    /// Plutôt que de reconstruire une liste de sorts — ce qui obligerait à recopier les conditions
    /// de déblocage de chaque sort, une douzaine de nœuds de compétence dispersés que le jeu peut
    /// changer à toute mise à jour — on ouvre LE menu du jeu et on y pose la sélection clavier.
    /// La liste affichée est donc exactement celle du jeu, et valider déclenche son propre code
    /// d'équipement.
    ///
    /// Une fois la sélection posée, plus rien de spécifique n'est nécessaire : les entrées sont des
    /// `ItemImage`, qui déclenchent l'infobulle native (lue par TooltipReader) et implémentent
    /// `ISubmitHandler` (donc la validation habituelle les équipe).
    /// </summary>
    public static class SpellEquipMenu
    {
        /// <summary>Emplacement en cours, de 0 à 3 ; -1 quand aucun menu n'est ouvert.</summary>
        private static int _slot = -1;

        private static FieldInfo _selectablesField;
        private static FieldInfo _slotField;
        private static bool _resolved;

        /// <summary>
        /// Passe à l'emplacement suivant, en ouvrant son choix de sort. Après le dernier, ferme.
        ///
        /// Un seul geste répété plutôt qu'un sous-menu « choisissez d'abord un emplacement » : on
        /// équipe ses sorts rarement, et une liste de quatre entrées ne mérite pas un niveau de
        /// navigation supplémentaire.
        /// </summary>
        public static void Advance()
        {
            List<SpellSelection> panels = Panels();
            if (panels.Count == 0)
            {
                TolkSpeech.Speak("Le choix des sorts n'est pas disponible ici.", true);
                return;
            }

            _slot++;
            if (_slot >= panels.Count)
            {
                CloseAll(panels);
                _slot = -1;
                TolkSpeech.Speak("Choix des sorts fermé.", true);
                return;
            }

            CloseAll(panels);
            SpellSelection panel = panels[_slot];

            try { panel.ShowSpellSelection(); }
            catch
            {
                TolkSpeech.Speak("Impossible d'ouvrir le choix des sorts.", true);
                _slot = -1;
                return;
            }

            List<Selectable> entries = EntriesOf(panel);
            if (entries.Count == 0)
            {
                TolkSpeech.Speak($"Emplacement {SlotNumber(panel, _slot)} : aucun sort disponible.", true);
                return;
            }

            // Le préfixe passe par FocusReader : l'infobulle du sort est annoncée à la frame
            // suivante en coupant la parole, donc une annonce faite ici serait avalée.
            FocusReader.SetPendingPrefix(
                $"Emplacement {SlotNumber(panel, _slot)}, {entries.Count} sort{(entries.Count > 1 ? "s" : "")} disponible{(entries.Count > 1 ? "s" : "")}");

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(entries[0].gameObject);
        }

        /// <summary>Referme tout choix de sort ouvert. Appelée aussi quand on quitte l'écran.</summary>
        public static void Close()
        {
            if (_slot < 0) return;
            CloseAll(Panels());
            _slot = -1;
        }

        // ------------------------------------------------------------------ Interne

        private static List<SpellSelection> Panels()
        {
            try
            {
                // Triés de gauche à droite : c'est l'ordre visuel des emplacements, et donc celui
                // auquel s'attend le joueur — l'ordre de découverte par Unity, lui, est arbitraire.
                return Object.FindObjectsOfType<SpellSelection>()
                    .Where(p => p != null && p.gameObject.activeInHierarchy)
                    .OrderBy(p => p.transform.position.x)
                    .ToList();
            }
            catch { return new List<SpellSelection>(); }
        }

        private static void CloseAll(List<SpellSelection> panels)
        {
            foreach (SpellSelection panel in panels)
            {
                try { panel.HideSpellSelection(); } catch { }
            }
        }

        /// <summary>
        /// Les entrées du choix ouvert. Le jeu les garde dans une liste privée qu'il vient de
        /// remplir : la lire évite de deviner quels sorts sont débloqués.
        /// </summary>
        private static List<Selectable> EntriesOf(SpellSelection panel)
        {
            Resolve();
            if (_selectablesField == null) return new List<Selectable>();

            try
            {
                var list = _selectablesField.GetValue(panel) as List<Selectable>;
                return list?.Where(s => s != null && s.gameObject.activeInHierarchy).ToList()
                       ?? new List<Selectable>();
            }
            catch { return new List<Selectable>(); }
        }

        /// <summary>
        /// Numéro d'emplacement affiché. Le jeu le stocke dans un champ privé ; à défaut, le rang
        /// visuel fait un repli honnête plutôt qu'un numéro inventé.
        /// </summary>
        private static int SlotNumber(SpellSelection panel, int fallbackIndex)
        {
            Resolve();
            if (_slotField != null)
            {
                try { return (int)_slotField.GetValue(panel); }
                catch { }
            }
            return fallbackIndex + 1;
        }

        private static void Resolve()
        {
            if (_resolved) return;
            _resolved = true;

            _selectablesField = typeof(SpellSelection).GetField("selectables",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _slotField = typeof(SpellSelection).GetField("spellSlot",
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (_selectablesField == null)
                Plugin.Log?.LogWarning("SpellSelection.selectables introuvable : le choix des sorts au clavier sera inactif.");
        }
    }
}
