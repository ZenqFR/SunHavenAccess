using HarmonyLib;
using UnityEngine.EventSystems;
using DG.Tweening.Core;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Fait tourner AccessibilityRunner.Tick() en s'accrochant à des méthodes Update() natives
    /// déjà correctement enregistrées dans la boucle de jeu depuis le tout début — contrairement
    /// à un MonoBehaviour créé par notre propre plugin (voir AccessibilityRunner pour
    /// l'explication complète du problème que ceci contourne). Deux points d'accroche
    /// redondants au cas où l'un des deux ne serait pas encore actif à un moment donné
    /// (EventSystem n'existe que s'il y a une UI ; DOTweenComponent tourne dès qu'une
    /// animation a été lancée, ce qui est très tôt dans ce jeu).
    /// </summary>
    /// <summary>
    /// Le délégué est construit UNE FOIS, pas à chaque image.
    ///
    /// `PatchGuard.Run("TickDriver", AccessibilityRunner.Tick)` a l'air gratuit, mais passer un
    /// nom de méthode là où un délégué est attendu en crée un neuf à chaque appel. Sur les deux
    /// points d'accroche, cela faisait cent vingt petites allocations par seconde, indéfiniment,
    /// pour toujours appeler la même chose.
    /// </summary>
    internal static class TickDriverDelegate
    {
        internal static readonly System.Action Tick = AccessibilityRunner.Tick;
    }

    [HarmonyPatch(typeof(EventSystem), "Update")]
    public static class TickDriverPatch_EventSystem
    {
        private static void Postfix()
        {
            PatchGuard.Run("TickDriver", TickDriverDelegate.Tick);
        }
    }

    [HarmonyPatch(typeof(DOTweenComponent), "Update")]
    public static class TickDriverPatch_DOTween
    {
        private static void Postfix()
        {
            PatchGuard.Run("TickDriver", TickDriverDelegate.Tick);
        }
    }
}
