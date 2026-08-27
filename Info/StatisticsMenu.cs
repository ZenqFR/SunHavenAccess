using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SunHavenAccess.Menus;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// L'onglet Statistiques en liste.
    ///
    /// C'est le seul panneau du menu principal qui n'a aucune donnée exploitable derrière lui :
    /// il ne contient que du texte mis en page — des intitulés à gauche, des valeurs à droite,
    /// appariés par leur position et rien d'autre. Il n'existe pas de « liste de statistiques »
    /// dans le code du jeu à laquelle se raccrocher.
    ///
    /// On lit donc ce qui est affiché, mais on le présente comme une liste plutôt qu'en suivant
    /// la mise en page. Les lignes voisines en hauteur sont réunies : c'est ainsi qu'un intitulé
    /// retrouve sa valeur, sans avoir à supposer quoi que ce soit de la structure du panneau.
    /// </summary>
    public static class StatisticsMenu
    {
        /// <summary>
        /// Écart de hauteur en deçà duquel deux textes sont considérés sur la même ligne. Exprimé
        /// en fraction de la hauteur totale occupée par le panneau, jamais en pixels : une valeur
        /// absolue dépendrait de la résolution et du facteur d'échelle de l'interface.
        /// </summary>
        private const float SameRowFraction = 0.02f;

        public static void Open()
        {
            List<TMPro.TextMeshProUGUI> texts = VisibleTexts();
            if (texts.Count == 0)
            {
                TolkSpeech.Speak("Aucune statistique à afficher.", true);
                return;
            }

            ListMenu.Open("Statistiques", BuildRows(texts));
        }

        /// <summary>
        /// Regroupe les textes par ligne, de haut en bas, chaque ligne lue de gauche à droite.
        /// Un intitulé et sa valeur se retrouvent ainsi dans la même entrée — « Récoltes : 142 »
        /// plutôt que deux entrées séparées dont la seconde ne voudrait rien dire.
        /// </summary>
        private static List<string> BuildRows(List<TMPro.TextMeshProUGUI> texts)
        {
            var sorted = texts.OrderByDescending(t => t.transform.position.y).ToList();

            float top = sorted[0].transform.position.y;
            float bottom = sorted[sorted.Count - 1].transform.position.y;
            float tolerance = Mathf.Max((top - bottom) * SameRowFraction, 0.01f);

            var rows = new List<string>();
            var current = new List<TMPro.TextMeshProUGUI> { sorted[0] };

            for (int i = 1; i < sorted.Count; i++)
            {
                bool sameRow = Mathf.Abs(sorted[i].transform.position.y - current[0].transform.position.y) <= tolerance;
                if (!sameRow)
                {
                    rows.Add(Join(current));
                    current = new List<TMPro.TextMeshProUGUI>();
                }
                current.Add(sorted[i]);
            }
            rows.Add(Join(current));

            return rows.Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        }

        private static string Join(List<TMPro.TextMeshProUGUI> row) =>
            string.Join(" : ", row
                .OrderBy(t => t.transform.position.x)
                .Select(t => TextUtil.Clean(t.text))
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        /// <summary>
        /// Les textes réellement à l'écran. Le filtre de présence est indispensable : le jeu garde
        /// les panneaux des autres onglets actifs et opaques, rangés hors champ, et sans lui on
        /// ramasserait leurs textes en plus des statistiques.
        /// </summary>
        private static List<TMPro.TextMeshProUGUI> VisibleTexts()
        {
            try
            {
                return Object.FindObjectsOfType<TMPro.TextMeshProUGUI>()
                    .Where(t => t != null
                                && t.gameObject.activeInHierarchy
                                && !string.IsNullOrWhiteSpace(t.text)
                                && t.text.Trim().Length > 1
                                && MenuNavigator.IsOnScreen(t))
                    .ToList();
            }
            catch { return new List<TMPro.TextMeshProUGUI>(); }
        }
    }
}
