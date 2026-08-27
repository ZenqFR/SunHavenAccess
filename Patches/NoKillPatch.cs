using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Sun Haven détruit lui-même (confirmé empiriquement : Plugin.OnDestroy est appelé
    /// très tôt) l'objet gestionnaire de BepInEx, qui héberge tous les plugins dont le nôtre —
    /// exactement le problème que corrige le mod communautaire "Keep Alive" ("sans lui, les
    /// mods se déchargent au retour au menu principal"). On protège ici directement, par
    /// référence d'objet (fiable, contrairement à un filtrage par nom qui s'est révélé
    /// insuffisant) : toute tentative de détruire l'objet racine de notre propre plugin est
    /// interceptée et annulée.
    /// </summary>
    public static class NoKillPatch
    {
        /// <summary>À renseigner par Plugin.Awake() dès que possible.</summary>
        public static GameObject ProtectedRoot;

        private static bool IsProtected(Object obj)
        {
            if (ProtectedRoot == null || obj == null) return false;
            if (obj is GameObject go) return go == ProtectedRoot;
            if (obj is Component c) return c.gameObject == ProtectedRoot;
            return false;
        }

        [HarmonyPatch(typeof(Object), nameof(Object.Destroy), new System.Type[] { typeof(Object) })]
        public static class DestroyPatch
        {
            private static bool Prefix(Object obj)
            {
                if (IsProtected(obj))
                {
                    Plugin.Log?.LogInfo("Destruction de l'objet du mod bloquée (protection active).");
                    return false; // annule l'appel d'origine
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Object), nameof(Object.Destroy), new System.Type[] { typeof(Object), typeof(float) })]
        public static class DestroyDelayedPatch
        {
            private static bool Prefix(Object obj)
            {
                if (IsProtected(obj))
                {
                    Plugin.Log?.LogInfo("Destruction différée de l'objet du mod bloquée (protection active).");
                    return false;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Repli complémentaire : masque aussi l'objet protégé des scans de "ménage" du jeu basés
    /// sur Scene.GetRootGameObjects(), au cas où le mécanisme de nettoyage ne passe pas par
    /// Object.Destroy directement (par ex. SetActive(false) puis destruction différée ailleurs).
    /// </summary>
    [HarmonyPatch(typeof(Scene), nameof(Scene.GetRootGameObjects), new System.Type[] { })]
    public static class NoKillScanPatch
    {
        private static void Postfix(ref GameObject[] __result)
        {
            if (__result == null || NoKillPatch.ProtectedRoot == null) return;

            // On ne reconstruit le tableau que si l'objet protégé s'y trouve VRAIMENT.
            //
            // `Scene.GetRootGameObjects` est appelée très souvent par le moteur et par le jeu ;
            // reconstruire systématiquement allouait un tableau neuf à chaque appel, pour un
            // résultat presque toujours identique à l'entrée. Un parcours sans allocation coûte
            // infiniment moins qu'une allocation permanente sur un chemin aussi chaud — le mod ne
            // doit rien coûter à un joueur qui, lui, ne s'en sert pas.
            for (int i = 0; i < __result.Length; i++)
            {
                if (__result[i] != NoKillPatch.ProtectedRoot) continue;
                __result = __result.Where(go => go != NoKillPatch.ProtectedRoot).ToArray();
                return;
            }
        }
    }

    [HarmonyPatch(typeof(Scene), nameof(Scene.GetRootGameObjects), new System.Type[] { typeof(List<GameObject>) })]
    public static class NoKillScanPatchList
    {
        private static void Postfix(List<GameObject> rootGameObjects)
        {
            if (rootGameObjects == null || NoKillPatch.ProtectedRoot == null) return;
            rootGameObjects.RemoveAll(go => go == NoKillPatch.ProtectedRoot);
        }
    }
}
