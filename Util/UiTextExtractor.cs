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

            string skillNode = TrySkillNodeDescription(go);
            if (skillNode != null) return skillNode;

            string saveDescription = TrySavePanelDescription(go);
            if (saveDescription != null) return saveDescription;

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
        ///
        /// Bug corrigé : le dernier onglet (Paramètres) s'est remis à se lire comme le nom brut
        /// non traduit ("Major..."). Cause : cette liste comptait TOUS les frères nommés
        /// "Major*" sans filtrer les INACTIFS — un objet gabarit/dupliqué désactivé mais toujours
        /// présent dans la hiérarchie (fréquent pour les prefabs d'instanciation) se glissait
        /// dans le compte et décalait le rang du 7e onglet réel à 8, hors de
        /// la table des libellés → retombait en dernier repli sur la traduction
        /// littérale du nom technique. Filtré aux frères réellement actifs à l'écran, comme
        /// partout ailleurs dans le mod (Scanner, MenuNavigator...).
        /// </summary>
        private static string TryMajorTabLabel(GameObject go)
        {
            // Source AUTORITATIVE en priorité : la position de cet onglet dans la liste `tabs` du
            // jeu EST son index de panneau. Le calcul de rang ci-dessous n'est qu'un repli pour
            // les cas où cette liste n'est pas accessible (hors partie, menu pas encore
            // initialisé) — c'est lui qui pouvait se décaler et annoncer un mauvais onglet.
            string authoritative = Menus.ZoneNavigator.TabLabelFor(go);
            if (authoritative != null) return authoritative;

            if (!UiNameTranslator.IsMajorTabName(go.name)) return null;
            Transform parent = go.transform.parent;
            if (parent == null) return null;

            var majorSiblings = new List<Transform>();
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (UiNameTranslator.IsMajorTabName(child.name) && child.gameObject.activeInHierarchy)
                    majorSiblings.Add(child);
            }

            int rank = majorSiblings.IndexOf(go.transform); // 0-based, aligné sur majorTabIndex
            if (rank < 0) return null;
            if (UiNameTranslator.MajorTabLabelsByIndex.TryGetValue(rank, out string label)) return label;

            // Filet de sécurité : si le rang tombe hors de la table (nombre d'onglets inattendu),
            // annoncer un numéro générique plutôt que de retomber sur le nom technique brut.
            return $"Onglet {rank + 1}";
        }

        /// <summary>
        /// Un nœud de l'arbre de compétences. Sans ce cas particulier, un nœud n'est qu'une icône
        /// sans texte visible : le repli générique ne trouvait rien et retombait sur le nom
        /// technique de l'objet Unity, ce qui ne dit ni ce que fait la compétence, ni où on en est,
        /// ni si on peut la prendre. L'arbre était donc parcourable mais illisible.
        ///
        /// Ce que le jeu montre visuellement — l'icône grisée d'un nœud verrouillé, le compteur de
        /// rang, l'infobulle — est ici mis en mots dans l'ordre où il sert : ce que c'est, où on en
        /// est, si c'est accessible, puis l'effet.
        /// </summary>
        private static string TrySkillNodeDescription(GameObject go)
        {
            SkillNode node = go.GetComponentInParent<SkillNode>();
            if (node == null) return null;

            var parts = new List<string>();

            string title = SafeText(() => node.nodeTitle) ?? SafeText(() => node.nodeName);
            parts.Add(string.IsNullOrWhiteSpace(title) ? "Compétence" : title);

            // Progression. Un nœud à rangs multiples n'a de sens qu'avec les deux nombres ; un
            // nœud simple se dit « pris » ou « non pris », un « 0 sur 1 » n'apprend rien.
            int amount = node.NodeAmount;
            int max = SafeInt(() => node.nodePoints, 1);
            if (max > 1) parts.Add($"rang {amount} sur {max}");
            else parts.Add(amount > 0 ? "prise" : "non prise");

            // Disponibilité : `Available` est privée. On la LIT plutôt que de recalculer le seuil
            // de points par palier — dupliquer une règle du jeu, c'est accepter qu'elle diverge le
            // jour où le jeu la change.
            bool? available = ReadAvailable(node);
            if (available == false)
            {
                int tier = SafeInt(() => node.tier, 0);
                parts.Add(tier > 1
                    ? $"verrouillée, demande {5 * (tier - 1)} points dépensés dans cet arbre"
                    : "verrouillée");
            }
            else if (amount >= max)
            {
                parts.Add("terminée");
            }

            string description = SafeText(() => node.description);
            if (!string.IsNullOrWhiteSpace(description)) parts.Add(TextUtil.Clean(description));

            return string.Join(", ", parts) + ".";
        }

        /// <summary>
        /// `SkillNode.Available` est une propriété privée. Accesseur mis en cache : la résolution
        /// par réflexion coûte cher et cette méthode est appelée à chaque déplacement dans l'arbre.
        /// Retourne null si la propriété a disparu, auquel cas la disponibilité est simplement
        /// passée sous silence plutôt qu'annoncée à tort.
        /// </summary>
        private static System.Reflection.PropertyInfo _availableProperty;
        private static bool _availableResolved;

        private static bool? ReadAvailable(SkillNode node)
        {
            if (!_availableResolved)
            {
                _availableResolved = true;
                _availableProperty = typeof(SkillNode).GetProperty("Available",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (_availableProperty == null)
                    Plugin.Log?.LogWarning("SkillNode.Available introuvable : la disponibilité des compétences ne sera pas annoncée.");
            }

            if (_availableProperty == null) return null;
            try { return (bool)_availableProperty.GetValue(node); }
            catch { return null; }
        }

        /// <summary>
        /// Les propriétés de SkillNode passent par la localisation et les données d'asset : sur un
        /// nœud pas encore initialisé, elles lèvent. Un nœud incomplet doit rester lisible pour ce
        /// qu'il a, pas faire échouer toute l'annonce.
        /// </summary>
        private static string SafeText(System.Func<string> read)
        {
            try { return read(); }
            catch { return null; }
        }

        private static int SafeInt(System.Func<int> read, int fallback)
        {
            try { return read(); }
            catch { return fallback; }
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

            // Emplacement de paquet (musée, autel...) : traité AVANT le test « vide », car c'est
            // justement vide qu'il porte l'information utile — ce qu'il attend encore.
            string bundle = Info.BundleReader.DescribeSlot(slot);
            if (bundle != null) return bundle;

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

        /// <summary>
        /// Écran de sélection de sauvegarde (Wish.SavePanel, un slot de sauvegarde) : nom du
        /// personnage, jour, niveaux de compétence et argent sont chacun un champ TMP PUBLIC
        /// séparé sur la classe, pas forcément tous frères directs du bouton "Choisir" dans la
        /// hiérarchie Unity réelle (inconnue sans avoir vu la scène) — la recherche générique de
        /// frères directs risquerait donc de rater une partie de l'info. On lit ici directement
        /// tous les champs publics de SavePanel plutôt que de deviner la structure visuelle.
        /// Seul le bouton "Choisir" (selectButton) déclenche ce résumé complet ; Supprimer et
        /// Restaurer gardent la lecture générique (leur propre libellé, ex. "Supprimer").
        /// </summary>
        private static string TrySavePanelDescription(GameObject go)
        {
            SavePanel panel = go.GetComponentInParent<SavePanel>();
            if (panel == null) return null;

            UnityEngine.UI.Button button = go.GetComponentInParent<UnityEngine.UI.Button>();
            if (button == null || button != panel.selectButton) return null;

            string name = TextUtil.Clean(panel.playerNameText?.text);
            if (string.IsNullOrWhiteSpace(name)) return null; // slot vide ou prefab gabarit désactivé

            var parts = new List<string> { name };

            string day = TextUtil.Clean(panel.dayYearText?.text);
            if (!string.IsNullOrWhiteSpace(day)) parts.Add(day);

            var levels = new List<string>();
            void AddLevel(string label, TextMeshProUGUI tmp)
            {
                string value = TextUtil.Clean(tmp?.text);
                if (!string.IsNullOrWhiteSpace(value)) levels.Add($"{label} {value}");
            }
            AddLevel("Combat", panel.combatLevelText);
            AddLevel("Agriculture", panel.farmingLevelText);
            AddLevel("Pêche", panel.fishingLevelText);
            AddLevel("Minage", panel.miningLevelText);
            AddLevel("Exploration", panel.explorationLevelText);
            if (levels.Count > 0) parts.Add(string.Join(", ", levels));

            string coins = TextUtil.Clean(panel.coinText?.text);
            if (!string.IsNullOrWhiteSpace(coins)) parts.Add($"{coins} pièces d'or");

            return string.Join(". ", parts) + ".";
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
