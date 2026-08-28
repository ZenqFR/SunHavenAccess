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
    [HarmonyPatch(typeof(EventSystem), "Update")]
    public static class TickDriverPatch_EventSystem
    {
        private static void Postfix()
        {
            PatchGuard.Run("TickDriver", AccessibilityRunner.Tick);
        }
    }

    [HarmonyPatch(typeof(DOTweenComponent), "Update")]
    public static class TickDriverPatch_DOTween
    {
        private static void Postfix()
        {
            PatchGuard.Run("TickDriver", AccessibilityRunner.Tick);
        }
    }
}
