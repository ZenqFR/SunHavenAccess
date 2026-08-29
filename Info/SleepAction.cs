using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Aller se coucher, en un geste.
    ///
    /// Terminer sa journée est le geste le plus répété du jeu, et c'était l'un des plus pénibles :
    /// changer de catégorie jusqu'aux services, parcourir jusqu'au lit, lancer le trajet, puis
    /// trouver la touche d'interaction. Quatre étapes, tous les soirs, pour une action qui n'a
    /// jamais d'alternative.
    ///
    /// Le mod fait le chemin et actionne le lit. Ce qui suit — la confirmation, le bilan de la
    /// journée — est déjà lu : la confirmation passe par les bulles de dialogue, et le bilan par
    /// le patch sur l'écran de fin de journée.
    ///
    /// CE QU'IL NE FAIT PAS. Aller dormir depuis une autre zone. Un lit n'existe pour le jeu que
    /// dans la zone où il se trouve : depuis les champs, on ne peut pas viser un objet qui n'est
    /// pas chargé. On dit alors où est l'entrée de la maison plutôt que de faire semblant.
    /// </summary>
    internal static class SleepAction
    {
        internal static void GoToBed()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Le jeu n'est pas encore chargé.", "The game is not loaded yet."), true);
                return;
            }

            Bed bed = Nearest(player);
            if (bed != null)
            {
                // Le trajet mène au lit ; le coucher lui-même reste un geste volontaire, parce
                // qu'une journée se termine quand on le décide, pas quand on passe à côté du lit.
                PathingController.TravelTo(bed.transform.position, Localization.Language.T("le lit", "the bed"));

                string key = Input.GameKeys.NameFor(Button.Interact);
                TolkSpeech.Speak(string.IsNullOrEmpty(key)
                    ? Localization.Language.T(
                        "Trajet vers le lit. Utilisez la touche d'interaction en arrivant.",
                        "Walking to the bed. Use the interact key on arrival.")
                    : Localization.Language.T(
                        $"Trajet vers le lit. {key} en arrivant pour dormir.",
                        $"Walking to the bed. {key} on arrival to sleep."), false);
                return;
            }

            Suggest();
        }

        private static Bed Nearest(Player player)
        {
            try
            {
                return Object.FindObjectsOfType<Bed>()
                    .Where(b => b != null)
                    .OrderBy(b => Vector3.Distance(b.transform.position, player.transform.position))
                    .FirstOrDefault();
            }
            catch { return null; }
        }

        /// <summary>
        /// Pas de lit ici : on indique la sortie qui mène chez soi, quand on la trouve. Dire « pas
        /// de lit » et s'arrêter là laisserait exactement au même point qu'avant.
        /// </summary>
        private static void Suggest()
        {
            try
            {
                var home = Scanner.PortalsInScene()
                    .FirstOrDefault(p => p != null && IsPlayerHouse(p));

                if (home != null)
                {
                    PathingController.TravelTo(home.transform.position,
                        Localization.Language.T("votre maison", "your house"));
                    TolkSpeech.Speak(Localization.Language.T(
                        "Pas de lit ici : trajet vers votre maison. Redemandez une fois à l'intérieur.",
                        "No bed here: heading to your house. Ask again once inside."), false);
                    return;
                }
            }
            catch { }

            TolkSpeech.Speak(Localization.Language.T(
                "Pas de lit dans cette zone, et je ne vois pas l'entrée de votre maison d'ici.",
                "No bed in this area, and I can't see your house entrance from here."), true);
        }

        private static bool IsPlayerHouse(ScenePortalSpot portal)
        {
            try
            {
                var field = typeof(ScenePortalSpot).GetField("playerHousePortal",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(portal) is bool flag && flag;
            }
            catch { return false; }
        }
    }
}
