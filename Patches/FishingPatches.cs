using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Premiers repères vocaux pour la pêche (23/08/2026) : la pêche est un mini-jeu en temps
    /// réel (une jauge qui glisse, à stopper dans une zone précise — voir Wish.Bobber en
    /// décompilation) qui reste un vrai point faible d'accessibilité même pour stardew-access.
    /// Cette passe ne résout PAS le cœur du problème (viser la jauge à l'oreille, sans repère
    /// audio continu synchronisé) — voir README pour la limite assumée — mais donne au moins
    /// les repères qui manquaient totalement : savoir QU'UN poisson mord (pour réagir à temps),
    /// et le résultat de chaque pression (touché/manqué/échappé), là où c'était jusqu'ici
    /// entièrement silencieux côté accessibilité.
    /// </summary>
    [HarmonyPatch(typeof(Bobber), nameof(Bobber.Bite))]
    public static class FishBitePatch
    {
        private static void Postfix() => TolkSpeech.Speak("Ça mord !", interrupt: true);
    }

    /// <summary>
    /// Le résultat (`flag`, valeur de retour) indique si CETTE pression a touché la jauge dans
    /// la zone gagnante, pas si la pêche entière est terminée (un poisson peut demander
    /// plusieurs pressions à la suite, voir `queuedMinigames` en décompilation).
    /// </summary>
    [HarmonyPatch(typeof(Bobber), nameof(Bobber.FinishMiniGame))]
    public static class FishMinigameResultPatch
    {
        private static void Postfix(bool __result)
        {
            TolkSpeech.Speak(__result ? "Touché !" : "Manqué.", interrupt: true);
        }
    }

    [HarmonyPatch(typeof(Bobber), nameof(Bobber.FailMiniGame))]
    public static class FishMinigameFailPatch
    {
        private static void Postfix() => TolkSpeech.Speak("Le poisson s'est échappé.", interrupt: true);
    }
}
