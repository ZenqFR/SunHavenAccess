using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Premier lot de retours vocaux de combat (22/08/2026, début de la "suite du projet" après
    /// l'agriculture/les menus) : dégâts reçus par le joueur (avec alerte santé basse) et mort
    /// d'un ennemi. Volontairement PAS d'annonce à chaque coup PORTÉ par le joueur (spammerait
    /// énormément en combat rapide, coupant sans arrêt la synthèse vocale) — à ajouter plus tard
    /// si demandé, probablement avec `interrupt: false` et un anti-rebond court.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.ReceiveDamage))]
    public static class PlayerDamagePatch
    {
        private static void Postfix(Player __instance, DamageHit __result) =>
            PatchGuard.Run("PlayerDamage", () => Announce(__instance, __result));

        private static void Announce(Player __instance, DamageHit __result)
        {
            // Ce patch se déclenche pour TOUS les joueurs, pas seulement le vôtre : en
            // coopération, un coup encaissé par votre partenaire annonçait SA santé comme si
            // c'était la vôtre — de quoi croire qu'on est en train de mourir alors qu'on cultive
            // tranquillement. Les patches de récolte filtraient déjà de la sorte, via le paramètre
            // `hitFromLocalPlayer` que le jeu leur fournit ; ici il faut comparer nous-mêmes.
            if (__instance != Player.Instance) return;

            if (__result == null || !__result.hit) return;

            int health = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, __instance.Health));
            int maxHealth = UnityEngine.Mathf.CeilToInt(__instance.MaxHealth);
            float percentage = __instance.HealthPercentage;

            // Seul le complément d'alerte est traduit ici : le début de la phrase est reconnu par
            // motif dans Translator, qui reporte tel quel ce qui suit le point.
            string alerte = percentage <= 0f
                    ? SunHavenAccess.Localization.Language.T(" Vous êtes à terre !", " You are down!")
                : percentage <= 0.2f
                    ? SunHavenAccess.Localization.Language.T(" Attention, santé critique !", " Careful, health critical!")
                : percentage <= 0.4f
                    ? SunHavenAccess.Localization.Language.T(" Santé basse.", " Health low.")
                : "";

            TolkSpeech.Speak($"Touché ! Santé : {health} sur {maxHealth}.{alerte}", interrupt: true);
        }
    }

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Die))]
    public static class EnemyDeathPatch
    {
        private static void Postfix(EnemyAI __instance, bool fromLocalPlayer) =>
            PatchGuard.Run("EnemyDeath", () => Announce(__instance, fromLocalPlayer));

        private static void Announce(EnemyAI __instance, bool fromLocalPlayer)
        {
            // Le jeu indique lui-même qui a porté le coup fatal — même paramètre que celui déjà
            // utilisé par les patches de récolte. En coopération, sans ce test, un partenaire qui
            // combat produisait un flux continu d'annonces pendant qu'on cultivait tranquillement.
            if (!fromLocalPlayer) return;

            // Wish.NPCAI hérite de Wish.EnemyAI (voir Navigation/Scanner.cs) : en principe les
            // PNJ n'appellent jamais Die(), mais on exclut quand même par sécurité pour ne
            // jamais annoncer à tort "PNJ vaincu".
            if (__instance is NPCAI) return;

            string name = !string.IsNullOrWhiteSpace(__instance.enemyName)
                ? Util.UiNameTranslator.Translate(__instance.enemyName)
                : "Ennemi";
            TolkSpeech.Speak($"{name} vaincu.", interrupt: false);
        }
    }

    /// <summary>
    /// Player.Die() ne fait vraiment "mourir" le joueur (déclenche DeathRoutine, animation +
    /// changement de scène) que si `PlaySettings.allowDeath` est actif — sinon la santé est
    /// juste remise au max sans séquence de mort. On ne vérifie donc `__instance.Dying` qu'APRÈS
    /// l'appel (Postfix) pour n'annoncer que les morts réelles ; le cas "santé à 0 mais pas de
    /// mort" est déjà couvert par l'alerte "à terre" de PlayerDamagePatch.
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.Die))]
    public static class PlayerDeathPatch
    {
        private static void Postfix(Player __instance) =>
            PatchGuard.Run("PlayerDeath", () => Announce(__instance));

        private static void Announce(Player __instance)
        {
            // Même piège qu'au-dessus : sans ce test, la mort du partenaire s'annonçait
            // « Vous êtes tombé au combat ».
            if (__instance != Player.Instance) return;

            if (__instance.Dying)
            {
                TolkSpeech.Speak("Vous êtes tombé au combat.", interrupt: true);
            }
        }
    }
}
