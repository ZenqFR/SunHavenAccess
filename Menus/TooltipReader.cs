using System.Reflection;
using TMPro;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Menus
{
    /// <summary>
    /// Beaucoup d'éléments d'interface (emplacements d'inventaire, équipement, ingrédients
    /// d'artisanat...) n'affichent leur nom que dans l'infobulle native du jeu (Wish.Tooltip),
    /// un objet désactivé tant que la souris n'est pas dessus. On lit ici directement le
    /// contenu de cette infobulle dès qu'elle s'active : le nom exact, tel que le jeu le
    /// connaît. Si l'objet survolé est un Wish.ItemIcon (emplacement contenant un objet en
    /// pile), on préfixe aussi la quantité — visible en permanence sur l'icône, séparément de
    /// l'infobulle — dans UNE SEULE annonce ("7, Blé") plutôt que deux annonces qui se coupaient
    /// l'une l'autre.
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

        private static FieldInfo _currentHoveredIconField;

        /// <summary>
        /// La quantité (ex. "7") est affichée en permanence sur l'ItemIcon lui-même
        /// (ItemIcon._amountTMP), indépendamment de l'infobulle — vide si amount == 1.
        /// Tooltip.CurrentIcon ne référence PAS forcément l'ItemIcon (le jeu appelle
        /// item.GetToolTip(...), pas Tooltip.EnableTooltip(this) directement) : la source
        /// fiable est le champ privé statique ItemIcon._currentHoveredIcon, que le jeu met à
        /// jour lui-même à chaque fois qu'une infobulle d'objet s'ouvre.
        /// </summary>
        private static string GetQuantityPrefix()
        {
            if (_currentHoveredIconField == null)
            {
                _currentHoveredIconField = typeof(ItemIcon).GetField("_currentHoveredIcon",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

            var icon = _currentHoveredIconField?.GetValue(null) as ItemIcon;
            if (icon != null && icon.amount > 1)
            {
                return icon.amount.ToString();
            }
            return null;
        }
    }
}
