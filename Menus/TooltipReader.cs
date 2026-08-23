using System.Reflection;
using TMPro;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Beaucoup d'éléments d'interface (emplacements d'inventaire, équipement, ingrédients
    /// d'artisanat, icônes de boutique...) n'affichent leur nom que dans l'infobulle native du
    /// jeu (Wish.Tooltip), un objet désactivé tant que la souris n'est pas dessus. On lit ici
    /// directement le contenu de cette infobulle dès qu'elle s'active : le nom exact, tel que
    /// le jeu le connaît. Si l'objet survolé porte une quantité (empilable), on la préfixe dans
    /// UNE SEULE annonce ("7, Blé") plutôt que deux annonces qui se coupaient l'une l'autre.
    /// Deux classes DIFFÉRENTES du jeu déclenchent cette même infobulle native, chacune avec son
    /// propre champ statique de suivi du survol : Wish.ItemIcon pour l'inventaire/équipement, et
    /// Wish.ItemImage (une classe totalement séparée, trouvée en décompilant Wish.CraftingPanel/
    /// Wish.BuyableItem) pour les icônes d'ingrédients d'artisanat ET les icônes de boutique. Sans
    /// vérifier aussi ItemImage, la quantité manquait silencieusement dès qu'on survolait une
    /// icône d'artisanat ou de boutique plutôt qu'un emplacement d'inventaire.
    /// </summary>
    public static class TooltipReader
    {
        private static FieldInfo _descriptionField;
        private static bool _wasActive;
        private static string _lastText = "";

        public static void Tick()
        {
            Tooltip tooltip = Tooltip.Instance;
            if (tooltip == null)
            {
                _wasActive = false;
                return;
            }

            bool active = tooltip.gameObject.activeSelf;
            if (!active)
            {
                _wasActive = false;
                return;
            }

            if (_descriptionField == null)
            {
                _descriptionField = typeof(Tooltip).GetField("_descriptionTMP",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var tmp = _descriptionField?.GetValue(tooltip) as TextMeshProUGUI;
            if (tmp == null) return;

            string clean = TextUtil.Clean(tmp.text);
            if (string.IsNullOrWhiteSpace(clean)) return;

            string quantityPrefix = GetQuantityPrefix();
            string full = string.IsNullOrEmpty(quantityPrefix) ? clean : $"{quantityPrefix}, {clean}";

            // Annonce à l'ouverture de l'infobulle, ou si son contenu change alors qu'elle
            // reste affichée (ex. survol d'un autre emplacement sans fermeture entre-temps).
            if (!_wasActive || full != _lastText)
            {
                _lastText = full;
                TolkSpeech.Speak(full, interrupt: true);
            }
            _wasActive = true;
        }

        private static FieldInfo _currentHoveredItemIconField;
        private static FieldInfo _currentHoveredItemImageField;

        /// <summary>
        /// La quantité (ex. "7") est affichée en permanence sur l'icône elle-même,
        /// indépendamment de l'infobulle — vide/non pertinente si amount &lt;= 1.
        /// Tooltip.CurrentIcon ne référence PAS forcément l'objet réellement survolé (le jeu
        /// appelle item.GetToolTip(...) directement, pas Tooltip.EnableTooltip(this)) : la
        /// source fiable est le champ privé statique `_currentHoveredIcon` de la classe qui a
        /// déclenché le survol — ItemIcon (inventaire/équipement) OU ItemImage (artisanat,
        /// boutique), deux classes séparées avec chacune le leur.
        /// </summary>
        private static string GetQuantityPrefix()
        {
            _currentHoveredItemIconField ??= typeof(ItemIcon).GetField("_currentHoveredIcon",
                BindingFlags.NonPublic | BindingFlags.Static);
            var itemIcon = _currentHoveredItemIconField?.GetValue(null) as ItemIcon;
            if (itemIcon != null && itemIcon.amount > 1)
            {
                return itemIcon.amount.ToString();
            }

            _currentHoveredItemImageField ??= typeof(ItemImage).GetField("_currentHoveredIcon",
                BindingFlags.NonPublic | BindingFlags.Static);
            var itemImage = _currentHoveredItemImageField?.GetValue(null) as ItemImage;
            if (itemImage != null && itemImage.Amount > 1)
            {
                return itemImage.Amount.ToString();
            }

            return null;
        }
    }
}
