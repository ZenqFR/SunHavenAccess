using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Lit chaque ligne de dialogue dès qu'elle COMMENCE à s'afficher, plutôt que d'attendre
    /// la fin de l'effet machine à écrire (un joueur voyant n'a pas besoin d'attendre
    /// l'animation pour savoir ce qui est écrit — un joueur aveugle non plus).
    /// DialogueController.TextScroll est appelée une fois par ligne, avec le texte encore
    /// "brut" (jetons de remplacement non résolus, ex. nom du joueur) : on applique donc
    /// nous-mêmes DialogueHelper.Replace/ReplaceParent, exactement comme le fait le jeu juste
    /// après, pour lire le texte définitif tel qu'il va s'afficher.
    /// </summary>
    [HarmonyPatch(typeof(DialogueController), "TextScroll")]
    public static class DialogueLinePatch
    {
        private static string _lastSpoken = "";
        private static FieldInfo _nameTMPField;

        private static void Prefix(DialogueController __instance, string text, Dictionary<int, Response> options) =>
            PatchGuard.Run("DialogueLine", () => Announce(__instance, text, options));

        private static void Announce(DialogueController __instance, string text, Dictionary<int, Response> options)
        {
            string resolved = DialogueHelper.Replace(text);
            resolved = DialogueHelper.ReplaceParent(resolved);
            string clean = TextUtil.Clean(resolved);
            if (string.IsNullOrWhiteSpace(clean) || clean == _lastSpoken) return;
            _lastSpoken = clean;

            string speaker = GetSpeakerName(__instance);
            string toSpeak = string.IsNullOrWhiteSpace(speaker) ? clean : $"{speaker} : {clean}";

            // Les réponses possibles étaient reçues ici depuis toujours et purement ignorées : le
            // mod lisait la question sans jamais dire qu'il y avait un choix, ni lequel. On
            // découvrait donc l'existence des options en tâtonnant aux flèches.
            string choices = DescribeChoices(options);
            if (choices != null) toSpeak += " " + choices;

            TolkSpeech.Speak(toSpeak, interrupt: true);
        }

        /// <summary>
        /// Énonce les réponses proposées à la suite de la question.
        ///
        /// Elles sont dites d'emblée, plutôt que découvertes une à une en naviguant : un joueur
        /// voyant lit la question ET ses options d'un même regard, et deux ou trois réponses
        /// courtes tiennent dans la même phrase. Les numéroter permet en outre de savoir combien
        /// il y en a avant de commencer à choisir.
        ///
        /// `Response.responseText` est un délégué évalué à la demande (le jeu y met parfois du
        /// texte dépendant de l'état de la partie) : chaque appel est donc protégé isolément,
        /// pour qu'une seule réponse défaillante n'emporte pas toute la liste.
        /// </summary>
        private static string DescribeChoices(Dictionary<int, Response> options)
        {
            if (options == null || options.Count == 0) return null;

            var texts = new List<string>();
            foreach (KeyValuePair<int, Response> entry in options.OrderBy(o => o.Key))
            {
                string label = null;
                try { label = TextUtil.Clean(entry.Value?.responseText?.Invoke()); }
                catch { }

                if (string.IsNullOrWhiteSpace(label)) continue;
                texts.Add($"{texts.Count + 1}, {label}");
            }

            if (texts.Count == 0) return null;
            return $"{texts.Count} choix : {string.Join(" ; ", texts)}.";
        }

        private static string GetSpeakerName(DialogueController dc)
        {
            if (_nameTMPField == null)
            {
                _nameTMPField = typeof(DialogueController).GetField("_nameTMP",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }
            var tmp = _nameTMPField?.GetValue(dc) as TextMeshProUGUI;
            return tmp != null ? TextUtil.Clean(tmp.text) : null;
        }
    }
}
