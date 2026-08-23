using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wish;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// Rassemble TOUT le texte lisible d'un élément d'interface (titre, quantité, prix...),
    /// pas seulement le premier champ trouvé — utilisé par FocusReader, HoverReader et
    /// MenuNavigator pour ne rien laisser de côté.
    ///
    /// Beaucoup de boutons (icônes, onglets) n'ont pas leur libellé DANS leurs propres enfants :
    /// souvent, l'icône cliquable et le texte du libellé sont deux enfants SÉPARÉS d'un même
    /// parent (onglet = icône + texte côte à côte). On élargit donc la recherche aux frères et
    /// au parent avant d'abandonner. En tout dernier repli, on traduit le nom technique de
    /// l'objet plutôt que de le lire tel quel en anglais.
    /// </summary>
    public static class UiTextExtractor
    {
        public static string ExtractAll(GameObject go)
        {
            string majorTab = TryMajorTabLabel(go);
            if (majorTab != null) return majorTab;

            string slotDescription = TrySlotDescription(go);
            if (slotDescription != null) return slotDescription;

            string own = ExtractFrom(go);
            if (own != null) return own;

            // Repli : le libellé est peut-être un frère (même parent) plutôt qu'un enfant. On
            // regarde chaque frère DIRECT séparément (son propre sous-arbre seulement), PAS tout
            // le sous-arbre du parent d'un coup : sur certains écrans (menu principal), le parent
            // d'un bouton peut être un grand panneau contenant plein d'AUTRES éléments sans
            // rapport (crédits, autres boutons...) — chercher dans tout ce sous-arbre ramassait
            // leur texte au passage et donnait des annonces mélangées et incompréhensibles (ex.
            // le nom du studio lu à la place/en plus du bouton).
            Transform parent = go.transform.parent;
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform sibling = parent.GetChild(i);
                    if (sibling == go.transform) continue;
                    string text = ExtractFrom(sibling.gameObject);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            return UiNameTranslator.Translate(go.name);
        }

        /// <summary>
        /// Le menu principal (touche Tab) a 7 onglets nommés "MajorTabN" sans texte visible, et
        /// le chiffre N du nom brut ne correspond pas de façon fiable à l'ordre visuel réel. On
        /// se base donc sur la POSITION de l'objet parmi ses frères "Major" (ordre hiérarchique
        /// Unity = ordre visuel gauche à droite pour une rangée d'onglets), pas sur son nom.
        /// </summary>
        private static string TryMajorTabLabel(GameObject go)
        {
            if (!UiNameTranslator.IsMajorTabName(go.name)) return null;
            Transform parent = go.transform.parent;
            if (parent == null) return null;

            var majorSiblings = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (UiNameTranslator.IsMajorTabName(child.name)) majorSiblings.Add(child);
            }

            int rank = majorSiblings.IndexOf(go.transform) + 1; // 1-based
            return UiNameTranslator.MajorTabLabelsByRank.TryGetValue(rank, out string label) ? label : null;
        }

        /// <summary>
        /// Un emplacement d'inventaire/équipement (Wish.Slot ou Wish.ArmorSlot) VIDE n'a ni
        /// texte ni infobulle native (celle-ci ne se déclenche que pour un objet réel — voir
        /// Menus/TooltipReader.cs, qui prend le relais pour les emplacements occupés). On
        /// vérifie directement dans les données du jeu plutôt que de deviner depuis l'affichage.
        /// </summary>
        private static string TrySlotDescription(GameObject go)
        {
            Slot slot = go.GetComponentInParent<Slot>();
            if (slot == null) return null;

            bool empty;
            try
            {
                empty = slot.inventory == null
                    || slot.inventory.Items == null
                    || slot.slotNumber < 0
                    || slot.slotNumber >= slot.inventory.Items.Count
                    || slot.inventory.Items[slot.slotNumber].item == null
                    || slot.inventory.Items[slot.slotNumber].item.ID() == 0;
            }
            catch
            {
                return null; // structure inattendue : on retombe sur la lecture générique plutôt que de planter
            }

            if (!empty) return null;

            if (slot.requireArmorType)
            {
                string armorName = Utilities.TranslateArmorType(slot.acceptableArmorType.ToString());
                return string.IsNullOrWhiteSpace(armorName)
                    ? "Emplacement d'armure vide."
                    : $"Emplacement d'armure, {armorName}, vide.";
            }

            return "Emplacement vide.";
        }

        private static string ExtractFrom(GameObject go)
        {
            var parts = go.GetComponentsInChildren<TextMeshProUGUI>(includeInactive: false)
                .Select(t => TextUtil.Clean(t.text))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            if (parts.Count == 0)
            {
                parts = go.GetComponentsInChildren<Text>(includeInactive: false)
                    .Select(t => TextUtil.Clean(t.text))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            if (parts.Count == 0) return null;
            return string.Join(". ", parts.Distinct());
        }
    }
}
