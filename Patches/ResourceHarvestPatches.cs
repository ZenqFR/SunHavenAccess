using System.Reflection;
using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Retour vocal sur le minage et la coupe de bois — jusqu'ici totalement silencieux au-delà
    /// de la simple présence du rocher/arbre repérée par le scanner. Découverte en décompilant
    /// Wish.Rock/Wish.Wood : leur Hit(damage, power, ...) vérifie `power >= _requiredPower`
    /// (l'outil en main est-il assez puissant pour CET objet précis, ex. pioche en bois contre
    /// un "heavystone") — si non, RIEN ne se passe côté jeu à part une petite animation de
    /// rebond, sans aucun message. Un joueur aveugle frapperait donc indéfiniment sans jamais
    /// savoir que son outil est en cause plutôt que sa précision. On comble ce silence, et on
    /// confirme la casse effective (onRockBreak/onWoodBreak sont des évènements PAR INSTANCE,
    /// pas statiques comme Wish.Hoe.onHoe — un Harmony patch sur Hit/Die évite d'avoir à
    /// s'abonner à chaque rocher/arbre individuellement).
    /// </summary>
    [HarmonyPatch(typeof(Rock), "Hit")]
    public static class RockHitPatch
    {
        private static readonly FieldInfo RequiredPowerField =
            typeof(Rock).GetField("_requiredPower", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void Prefix(Rock __instance, float power, bool hitFromLocalPlayer)
        {
            if (!hitFromLocalPlayer || RequiredPowerField == null) return;
            float required = (float)RequiredPowerField.GetValue(__instance);
            if (power < required)
            {
                TolkSpeech.Speak("Pioche trop faible pour ce rocher.", interrupt: false);
            }
        }
    }

    [HarmonyPatch(typeof(Rock), "Die")]
    public static class RockDiePatch
    {
        // Passe l'état "déjà en train de mourir" du Prefix au Postfix pour ne parler qu'une
        // seule fois, même si Die() est rappelée après coup (le jeu ignore les appels suivants
        // via son propre garde `if (!Dying)`, mais un Postfix Harmony s'exécute quand même).
        private static bool _wasAlreadyDying;

        private static void Prefix(Rock __instance)
        {
            _wasAlreadyDying = __instance.Dying;
        }

        private static void Postfix(bool hitFromLocalPlayer) =>
            PatchGuard.Run("RockDie", () => Announce(hitFromLocalPlayer));

        private static void Announce(bool hitFromLocalPlayer)
        {
            if (_wasAlreadyDying || !hitFromLocalPlayer) return;
            TolkSpeech.Speak("Rocher brisé.", interrupt: false);
        }
    }

    [HarmonyPatch(typeof(Wood), "Hit")]
    public static class WoodHitPatch
    {
        private static readonly FieldInfo RequiredPowerField =
            typeof(Wood).GetField("_requiredPower", BindingFlags.NonPublic | BindingFlags.Instance);

        private static void Prefix(Wood __instance, float power, bool hitFromLocalPlayer)
        {
            if (!hitFromLocalPlayer || RequiredPowerField == null) return;
            float required = (float)RequiredPowerField.GetValue(__instance);
            if (power < required)
            {
                TolkSpeech.Speak("Hache trop faible pour cet arbre.", interrupt: false);
            }
        }
    }

    [HarmonyPatch(typeof(Wood), "Die")]
    public static class WoodDiePatch
    {
        private static bool _wasAlreadyDying;

        private static void Prefix(Wood __instance)
        {
            _wasAlreadyDying = __instance.Dying;
        }

        private static void Postfix(bool hitFromLocalPlayer) =>
            PatchGuard.Run("WoodDie", () => Announce(hitFromLocalPlayer));

        private static void Announce(bool hitFromLocalPlayer)
        {
            if (_wasAlreadyDying || !hitFromLocalPlayer) return;
            TolkSpeech.Speak("Arbre abattu.", interrupt: false);
        }
    }
}
