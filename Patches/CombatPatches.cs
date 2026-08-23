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
        private static void Postfix(Player __instance, DamageHit __result)
        {
            if (__result == null || !__result.hit) return;

            int health = UnityEngine.Mathf.CeilToInt(UnityEngine.Mathf.Max(0f, __instance.Health));
            int maxHealth = UnityEngine.Mathf.CeilToInt(__instance.MaxHealth);
            float percentage = __instance.HealthPercentage;

            string alerte = percentage <= 0f ? " Vous êtes à terre !"
                : percentage <= 0.2f ? " Attention, santé critique !"
                : percentage <= 0.4f ? " Santé basse."
                : "";

            TolkSpeech.Speak($"Touché ! Santé : {health} sur {maxHealth}.{alerte}", interrupt: true);
        }
    }

    [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Die))]
    public static class EnemyDeathPatch
    {
        private static void Postfix(EnemyAI __instance)
        {
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
        private static void Postfix(Player __instance)
        {
            if (__instance.Dying)
            {
                TolkSpeech.Speak("Vous êtes tombé au combat.", interrupt: true);
            }
        }
    }
}
