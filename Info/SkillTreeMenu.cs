using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Menus;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// L'arbre de compétences en deux listes : les métiers, puis les compétences du métier choisi.
    ///
    /// À l'écran, l'arbre est une grille de quarante icônes réparties en rangées thématiques, avec
    /// une ligne de paliers verrouillés au-dessus. Le sens y tient largement à la POSITION : la
    /// colonne dit le palier, la rangée dit la famille. Reproduire cette disposition au clavier
    /// oblige à s'en construire une image mentale — exactement ce qu'on ne peut pas faire sans la
    /// voir.
    ///
    /// Le contenu, lui, est une simple liste : des compétences, chacune avec un nom, un rang et
    /// une condition. On la présente donc telle quelle, et chaque entrée porte EN MOTS ce que la
    /// position portait à l'œil — la famille et le palier deviennent du texte.
    ///
    /// Les données viennent de `SkillNode`, pas de l'affichage : ce qui est lu est donc identique
    /// à ce que le jeu appliquerait, et ne dépend pas de l'onglet réellement ouvert à l'écran.
    /// </summary>
    public static class SkillTreeMenu
    {
        /// <summary>
        /// Ouvre la liste des métiers. Choisir un métier ouvre ses compétences.
        ///
        /// Deux étapes plutôt qu'une liste unique de deux cents entrées : on vient toujours pour
        /// UN métier, et parcourir les quatre autres au passage n'aurait aucun intérêt.
        /// </summary>
        public static void Open()
        {
            List<SkillNode> nodes = AllNodes();
            if (nodes.Count == 0)
            {
                TolkSpeech.Speak("Les compétences ne sont pas disponibles ici. Ouvrez l'arbre de compétences.", true);
                return;
            }

            List<ProfessionType> professions = nodes
                .Select(Profession)
                .Where(p => p.HasValue)
                .Select(p => p.Value)
                .Distinct()
                .OrderBy(ProfessionName)
                .ToList();

            if (professions.Count == 0)
            {
                TolkSpeech.Speak("Aucun métier trouvé.", true);
                return;
            }

            var labels = professions
                .Select(p => $"{ProfessionName(p)}, {AvailablePoints(p)} point{(AvailablePoints(p) > 1 ? "s" : "")} " +
                             Localization.Language.T("à dépenser", "to spend"))
                .ToList();

            ListMenu.Open("Métiers", labels, chosen => OpenProfession(professions[chosen]));
        }

        /// <summary>
        /// Les compétences d'un métier, dans l'ordre des paliers puis des familles — c'est l'ordre
        /// dans lequel on les débloque, donc celui qui a du sens quand on cherche quoi prendre.
        /// </summary>
        private static void OpenProfession(ProfessionType profession)
        {
            List<SkillNode> nodes = AllNodes()
                .Where(n => Profession(n) == profession)
                .OrderBy(n => Safe(() => n.tier, 0))
                .ThenBy(n => Safe(() => n.nodeTitle, string.Empty))
                .ToList();

            if (nodes.Count == 0)
            {
                TolkSpeech.Speak($"{ProfessionName(profession)} : aucune compétence trouvée.", true);
                return;
            }

            var labels = nodes.Select(Describe).ToList();
            ListMenu.Open(ProfessionName(profession), labels);
        }

        /// <summary>
        /// Une compétence en une phrase : ce que c'est, où on en est, si on peut la prendre, et ce
        /// qu'elle fait. Le palier remplace la colonne, invisible sans la vue.
        /// </summary>
        private static string Describe(SkillNode node)
        {
            var parts = new List<string>();

            string title = Safe(() => node.nodeTitle, null) ?? Safe(() => node.nodeName, null);
            parts.Add(string.IsNullOrWhiteSpace(title)
                ? Localization.Language.T("Compétence", "Skill")
                : title);

            int tier = Safe(() => node.tier, 0);
            if (tier > 0) parts.Add(Localization.Language.T($"palier {tier}", $"tier {tier}"));

            int amount = Safe(() => node.NodeAmount, 0);
            int max = Safe(() => node.nodePoints, 1);
            parts.Add(max > 1
                ? Localization.Language.T($"rang {amount} sur {max}", $"rank {amount} of {max}")
                : Localization.Language.T(amount > 0 ? "prise" : "non prise",
                                          amount > 0 ? "taken" : "not taken"));

            if (amount >= max && max > 0) parts.Add(Localization.Language.T("terminée", "complete"));
            else if (tier > 1) parts.Add(Localization.Language.T(
                $"demande {5 * (tier - 1)} points dépensés dans ce métier",
                $"requires {5 * (tier - 1)} points spent in this profession"));

            string description = TextUtil.Clean(Safe(() => node.description, null));
            if (!string.IsNullOrWhiteSpace(description)) parts.Add(description);

            return string.Join(", ", parts) + ".";
        }

        // ------------------------------------------------------------------ Données

        /// <summary>
        /// Tous les nœuds de la scène, y compris ceux des métiers non affichés : c'est justement
        /// l'intérêt de lire les données plutôt que l'écran — on peut consulter n'importe quel
        /// métier sans avoir à ouvrir son onglet.
        /// </summary>
        private static List<SkillNode> AllNodes()
        {
            try
            {
                return Object.FindObjectsOfType<SkillNode>(includeInactive: true)
                    .Where(n => n != null)
                    .ToList();
            }
            catch { return new List<SkillNode>(); }
        }

        private static ProfessionType? Profession(SkillNode node)
        {
            try { return node.profession; }
            catch { return null; }
        }

        private static int AvailablePoints(ProfessionType profession)
        {
            // Le jeu n'expose pas un « points restants » : il donne le total gagné et le total
            // dépensé. La soustraction est la même que celle qu'il fait lui-même pour griser un
            // nœud (voir Skills.AvailableSkillPoint).
            try { return Mathf.Max(0, Skills.NumberOfSkillPoints(profession) - Skills.NumberOfSkillPointsSpent(profession)); }
            catch { return 0; }
        }

        private static string ProfessionName(ProfessionType profession)
        {
            switch (profession)
            {
                case ProfessionType.Farming:     return Localization.Language.T("Agriculture", "Farming");
                case ProfessionType.Mining:      return Localization.Language.T("Minage", "Mining");
                case ProfessionType.Combat:      return "Combat";
                case ProfessionType.Fishing:     return Localization.Language.T("Pêche", "Fishing");
                case ProfessionType.Exploration: return "Exploration";
                default:                         return profession.ToString();
            }
        }

        /// <summary>
        /// Les propriétés de SkillNode passent par la localisation et les données d'asset : sur un
        /// nœud pas encore initialisé, elles lèvent. Un nœud incomplet doit rester lisible pour ce
        /// qu'il a, pas faire échouer toute la liste.
        /// </summary>
        private static T Safe<T>(System.Func<T> read, T fallback)
        {
            try { return read(); }
            catch { return fallback; }
        }
    }
}
