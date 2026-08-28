using HarmonyLib;
using I2.Loc;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Annonce le nom et la description d'un lieu de la carte du monde dès qu'il est ouvert
    /// (clic natif, ou Navigation/MapNavigator.cs au clavier). Postfix sur `Wish.Map.OpenLocation
    /// (string, LocationName)`, publique — le nom vient de `MapImage.location`/`locationKey`
    /// (champs publics), la description est le paramètre `text` déjà traduit par l'appelant
    /// (`MapImage.OpenLocation`, voir décompilation).
    /// </summary>
    [HarmonyPatch(typeof(Map), nameof(Map.OpenLocation), new[] { typeof(string), typeof(LocationName) })]
    public static class MapPatch
    {
        private static void Postfix(string text, LocationName location) =>
            PatchGuard.Run("MapLocation", () => Announce(text, location));

        private static void Announce(string text, LocationName location)
        {
            if (location?.mapImage == null) return;

            string name = TextUtil.Clean(LocalizeText.TranslateText(location.mapImage.locationKey, location.mapImage.location));
            string description = TextUtil.Clean(text);

            string sentence = string.IsNullOrWhiteSpace(description) ? $"{name}." : $"{name}. {description}";
            TolkSpeech.Speak(sentence, interrupt: true);
        }
    }
}
