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

            // Le texte COMPLET est toujours mémorisé, même en mode bref : c'est lui que relit la
            // touche « description complète » (voir LastFullText), y compris après la fermeture
            // de l'infobulle.
            bool changed = !_wasActive || full != _lastText;
            _lastText = full;
            _wasActive = true;
            if (!changed) return;

            TolkSpeech.Speak(BriefIfPossible(full, quantityPrefix), interrupt: true);
        }

        /// <summary>
        /// Dernier texte d'infobulle complet (nom + description), conservé même après fermeture
        /// pour la relecture à la demande.
        /// </summary>
        public static string LastFullText => _lastText;

        /// <summary>
        /// Déclare un texte long qu'un AUTRE lecteur vient d'abréger, pour que la touche
        /// « description complète » puisse le relire.
        ///
        /// La relecture était réservée aux infobulles, ce qui piégeait tout écran abrégeant ses
        /// annonces par ses propres moyens : le détail disparaissait sans aucun moyen de le
        /// retrouver. Les emplacements de sauvegarde sont le premier cas — cinq niveaux de métier
        /// et un montant d'or à chaque déplacement, ou rien du tout.
        /// </summary>
        public static void RememberFullText(string full)
        {
            if (string.IsNullOrWhiteSpace(full)) return;
            _lastText = full;
        }

        /// <summary>Le mode bref est-il actif ? Piloté par la config (voir Plugin.Awake).</summary>
        public static bool BriefMode { get; set; } = true;

        /// <summary>
        /// En mode bref, remplace le texte fusionné nom+description de l'infobulle par le seul
        /// nom de l'objet (plus sa quantité). Le jeu ne met à disposition qu'UN champ contenant
        /// les deux collés, d'où le passage par `ItemIcon.itemData` (champ PUBLIC, déjà résolu
        /// par le jeu) pour obtenir le nom seul.
        ///
        /// Ne s'applique QU'AUX ItemIcon, jamais aux ItemImage : ces dernières sont les icônes
        /// d'artisanat et de boutique, où le prix et les ingrédients sont dans la description —
        /// l'abréger rendrait ces écrans inutilisables.
        ///
        /// Repli sur le texte complet dès que le nom n'est pas récupérable, plutôt que de risquer
        /// une annonce vide ou tronquée.
        /// </summary>
        private static string BriefIfPossible(string full, string quantityPrefix)
        {
            if (!BriefMode) return full;

            _currentHoveredItemIconField ??= typeof(ItemIcon).GetField("_currentHoveredIcon",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (_currentHoveredItemIconField?.GetValue(null) is not ItemIcon icon) return full;

            string name;
            try { name = TextUtil.Clean(icon.itemData?.UnformattedDisplayName); }
            catch { return full; }

            if (string.IsNullOrWhiteSpace(name)) return full;
            return string.IsNullOrEmpty(quantityPrefix) ? name : $"{quantityPrefix}, {name}";
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
