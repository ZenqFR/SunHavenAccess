using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Le bilan de fin de journée, lu à voix haute.
    ///
    /// CE QU'ON PERDAIT. Se coucher ouvre un écran qui récapitule la journée : pièces gagnées,
    /// billets, orbes, expérience par métier. C'est le seul moment où le jeu dit si la journée a
    /// servi à quelque chose — et il le disait en silence. On enchaînait les journées sans jamais
    /// savoir ce qu'elles avaient rapporté, ni si une récolte avait été vendue.
    ///
    /// COMMENT ON L'ATTRAPE. `DisplayAllCurrencySources` est appelée par le jeu au moment où il
    /// remplit ce bilan : on lit les totaux juste après, une fois qu'ils portent leurs vraies
    /// valeurs. Aucun guet, aucun sondage — l'écran prévient de lui-même.
    ///
    /// CE QU'ON DIT ET CE QU'ON TAIT. Seules les lignes non nulles sont annoncées : réciter
    /// « zéro billet, zéro orbe » à chaque coucher serait du bruit quotidien. Une journée sans
    /// aucun gain se dit en une phrase plutôt qu'en quatre zéros.
    /// </summary>
    [HarmonyPatch(typeof(EndOfDayScreen), nameof(EndOfDayScreen.DisplayAllCurrencySources))]
    public static class EndOfDayPatch
    {
        private static void Postfix(EndOfDayScreen __instance) =>
            PatchGuard.Run("BilanDeJournee", () => Announce(__instance));

        /// <summary>
        /// Les champs du bilan, avec leur libellé. Le jeu en tient deux jeux — un par colonne
        /// d'affichage — dont un seul est rempli selon la mise en page ; on lit les deux et l'on
        /// garde ce qui a une valeur, plutôt que de deviner lequel sert aujourd'hui.
        /// </summary>
        private static readonly (string Field, string Fr, string En)[] Totals =
        {
            ("coinsTotalTMP", "pièces", "coins"),
            ("coinsTotalTMP2", "pièces", "coins"),
            ("ticketsTotalTMP", "billets", "tickets"),
            ("ticketsTotalTMP2", "billets", "tickets"),
            ("orbsTotalTMP", "orbes", "orbs"),
            ("orbsTotalTMP2", "orbes", "orbs"),
            ("xpTotalTMP", "expérience", "experience"),
            ("xpTotalTMP2", "expérience", "experience"),
        };

        private static readonly Dictionary<string, FieldInfo> _fields = new Dictionary<string, FieldInfo>();

        private static void Announce(EndOfDayScreen screen)
        {
            var parts = new List<string>();
            var seen = new HashSet<string>();

            foreach ((string field, string fr, string en) in Totals)
            {
                string label = Localization.Language.T(fr, en);
                if (seen.Contains(label)) continue; // la colonne jumelle a déjà répondu

                string value = Read(screen, field);
                if (string.IsNullOrWhiteSpace(value) || IsZero(value)) continue;

                seen.Add(label);
                parts.Add($"{value} {label}");
            }

            TolkSpeech.Speak(parts.Count == 0
                ? Localization.Language.T(
                    "Fin de journée. Rien gagné aujourd'hui.",
                    "End of day. Nothing earned today.")
                : Localization.Language.T(
                    $"Fin de journée. {string.Join(", ", parts.ToArray())}.",
                    $"End of day. {string.Join(", ", parts.ToArray())}."), true);
        }

        private static string Read(EndOfDayScreen screen, string fieldName)
        {
            try
            {
                if (!_fields.TryGetValue(fieldName, out FieldInfo field))
                {
                    field = typeof(EndOfDayScreen).GetField(fieldName,
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    _fields[fieldName] = field;
                }

                var tmp = field?.GetValue(screen) as TextMeshProUGUI;
                return TextUtil.Clean(tmp?.text);
            }
            catch { return null; }
        }

        /// <summary>
        /// Une ligne à zéro n'apprend rien et se répète tous les soirs. On la retire, en acceptant
        /// les écritures du jeu — « 0 », « +0 », « 0 » avec séparateurs.
        /// </summary>
        private static bool IsZero(string value)
        {
            foreach (char c in value)
            {
                if (char.IsDigit(c) && c != '0') return false;
            }
            return true;
        }
    }
}
