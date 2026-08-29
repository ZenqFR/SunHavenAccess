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

        // Ces deux Prefix sont les SEULS du mod à ne pas passer par PatchGuard, et c'est délibéré.
        //
        // Ils sont greffés sur `Object.Destroy`, l'appel le plus fréquent du moteur : y ajouter une
        // fonction anonyme et un bloc de capture coûterait à chaque destruction d'objet du jeu.
        // Leur corps ne peut pas lever : `IsProtected` n'est qu'une comparaison de références avec
        // gardes de nullité, et le journal est appelé sur une référence conditionnelle. Un Prefix
        // qui renvoie un booléen demanderait de surcroît de décider quoi retourner en cas
        // d'erreur — et se tromper ici, c'est soit tuer le mod, soit empêcher le jeu de faire son
        // ménage.
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
        // Un paramètre `ref` interdit la garde partagée : on protège donc à la main. Cette méthode
        // est greffée sur un appel que le moteur fait très souvent — la laisser lever une exception
        // casserait le ménage de scène du jeu lui-même.
        private static void Postfix(ref GameObject[] __result)
        {
            try { Filter(ref __result); }
            catch (System.Exception e) { Plugin.Log?.LogWarning("NoKillScanPatch : " + e.Message); }
        }

        private static void Filter(ref GameObject[] __result)
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
        // Garde écrite à la main, pour la même raison que les deux Prefix sur `Object.Destroy`.
        //
        // `PatchGuard.Run(..., () => Filter(rootGameObjects))` semblait identique au reste du mod,
        // mais la fonction anonyme CAPTURE un paramètre : le compilateur alloue donc un objet de
        // capture et un délégué à CHAQUE appel. Or le moteur appelle `GetRootGameObjects` en
        // permanence — c'était des milliers d'allocations par seconde, uniquement pour permettre
        // au mod de vérifier qu'il n'y a rien à filtrer. Le ramasse-miettes finissait par se
        // déclencher, et c'est exactement ce qui se sent comme une saccade.
        //
        // Le mod ne doit rien coûter à qui ne s'en sert pas à cet instant.
        private static void Postfix(List<GameObject> rootGameObjects)
        {
            try { Filter(rootGameObjects); }
            catch (System.Exception e) { Plugin.Log?.LogWarning("NoKillScanList : " + e.Message); }
        }

        private static void Filter(List<GameObject> rootGameObjects)
        {
            if (rootGameObjects == null || NoKillPatch.ProtectedRoot == null) return;

            // Même prudence que pour l'autre surcharge : on ne remue la liste que si l'objet
            // protégé s'y trouve vraiment. `RemoveAll` parcourt et recompacte à chaque appel ;
            // une simple recherche coûte moins, et le cas « rien à retirer » est la règle.
            for (int i = 0; i < rootGameObjects.Count; i++)
            {
                if (rootGameObjects[i] != NoKillPatch.ProtectedRoot) continue;
                rootGameObjects.RemoveAt(i);
                return; // un objet racine n'apparaît qu'une fois dans sa scène
            }
        }
    }
}
