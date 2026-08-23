using System.Reflection;
using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Repères vocaux pour la pêche. La pêche est un mini-jeu en temps réel — voir Wish.Bobber
    /// en décompilation — qui reste un vrai point faible d'accessibilité même pour
    /// stardew-access. Découverte clé : contrairement à Stardew Valley (deux éléments mobiles
    /// indépendants à suivre, poisson ET barre), ici `miniGameSlider.value` (0 à 1) oscille TOUT
    /// SEUL en aller-retour automatique (tween Yoyo) — le joueur n'a qu'à appuyer au bon MOMENT
    /// pendant qu'elle traverse la zone gagnante (`winMin`/`winMax`). Un seul repère continu
    /// (la distance à cette zone) suffit donc à viser au son — voir Speech/FishingToneCue.cs
    /// pour le bip dont la hauteur varie en fonction de cette distance, le vrai cœur du
    /// problème que les annonces ponctuelles ci-dessous ne résolvaient pas à elles seules.
    /// </summary>
    [HarmonyPatch(typeof(Bobber), nameof(Bobber.Bite))]
    public static class FishBitePatch
    {
        private static void Postfix() => TolkSpeech.Speak("Ça mord !", interrupt: true);
    }

    /// <summary>
    /// Capture l'instance de Bobber dès qu'un mini-jeu démarre, pour que
    /// FishingToneDriver.Tick() (appelé chaque frame par AccessibilityRunner) puisse lire sa
    /// jauge et sa zone gagnante — surcharge désambiguïsée explicitement (StartMiniGame(FishData)
    /// existe aussi et appelle simplement celle-ci en interne).
    /// </summary>
    [HarmonyPatch(typeof(Bobber), nameof(Bobber.StartMiniGame), new System.Type[] { typeof(FishingMiniGame) })]
    public static class FishMinigameStartPatch
    {
        private static void Postfix(Bobber __instance)
        {
            FishingToneDriver.SetActiveBobber(__instance);
        }
    }

    /// <summary>
    /// Le résultat (`__result`) indique si CETTE pression a touché la jauge dans la zone
    /// gagnante, pas si la pêche entière est terminée (un poisson peut demander plusieurs
    /// pressions à la suite, voir `queuedMinigames` en décompilation) — d'où la vérification de
    /// `miniGameComplete` (paramètre de sortie) pour savoir s'il faut arrêter le repère sonore
    /// ou le laisser tourner pour la pression suivante.
    /// </summary>
    [HarmonyPatch(typeof(Bobber), nameof(Bobber.FinishMiniGame))]
    public static class FishMinigameResultPatch
    {
        private static void Postfix(bool __result, ref bool miniGameComplete)
        {
            TolkSpeech.Speak(__result ? "Touché !" : "Manqué.", interrupt: true);
            if (miniGameComplete) FishingToneDriver.SetActiveBobber(null);
        }
    }

    [HarmonyPatch(typeof(Bobber), nameof(Bobber.FailMiniGame))]
    public static class FishMinigameFailPatch
    {
        private static void Postfix()
        {
            TolkSpeech.Speak("Le poisson s'est échappé.", interrupt: true);
            FishingToneDriver.SetActiveBobber(null);
        }
    }

    [HarmonyPatch(typeof(Bobber), nameof(Bobber.ResetMiniGame))]
    public static class FishMinigameResetPatch
    {
        private static void Postfix() => FishingToneDriver.SetActiveBobber(null);
    }

    /// <summary>
    /// Pilote le bip continu de visée (Speech/FishingToneCue.cs) : lit chaque frame la jauge et
    /// la zone gagnante (`winMin`/`winMax`, privés — lus par réflexion, comme ailleurs dans le
    /// mod pour ce genre de champ interne) tant qu'un mini-jeu est en cours, et coupe le bip dès
    /// qu'il n'y en a plus.
    /// </summary>
    public static class FishingToneDriver
    {
        private static Bobber _activeBobber;
        private static FieldInfo _winMinField;
        private static FieldInfo _winMaxField;

        public static void SetActiveBobber(Bobber bobber)
        {
            _activeBobber = bobber;
            if (bobber == null) FishingToneCue.Stop();
        }

        public static void Tick()
        {
            if (_activeBobber == null || !_activeBobber.MiniGameInProgress)
            {
                if (_activeBobber != null) SetActiveBobber(null);
                return;
            }

            _winMinField ??= typeof(Bobber).GetField("winMin", BindingFlags.NonPublic | BindingFlags.Instance);
            _winMaxField ??= typeof(Bobber).GetField("winMax", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_winMinField == null || _winMaxField == null) return;

            float winMin = (float)_winMinField.GetValue(_activeBobber);
            float winMax = (float)_winMaxField.GetValue(_activeBobber);
            float sliderValue = _activeBobber.miniGameSlider.value;

            FishingToneCue.UpdateTarget(sliderValue, winMin, winMax);
        }
    }
}
