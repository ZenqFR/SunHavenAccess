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

            // LES RÉPONSES NE SONT PLUS RÉCITÉES ICI.
            //
            // Elles l'ont été un temps, à la suite de la question, pour qu'on sache au moins
            // qu'un choix existait. Mais un joueur voyant lit la question, puis baisse le regard
            // quand il a fini ; à l'oreille, tout arrivait dans une seule phrase, et la question
            // n'était pas terminée que les réponses défilaient déjà par-dessus. Signalé en jeu.
            //
            // Elles sont désormais dans une liste qu'on parcourt aux flèches, à son rythme, avec
            // une entrée pour relire la question — voir Dialogue/DialogueChoiceMenu.cs. On annonce
            // seulement COMBIEN il y en a, ce qui suffit à savoir qu'il faut répondre.
            int count = options?.Count ?? 0;
            if (count > 0)
            {
                toSpeak += Localization.Language.T(
                    $" {count} réponse{(count > 1 ? "s" : "")}, flèches pour les parcourir.",
                    $" {count} repl{(count > 1 ? "ies" : "y")}, arrows to browse them.");
            }

            TolkSpeech.Speak(toSpeak, interrupt: true);
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
