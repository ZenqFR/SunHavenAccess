using System;
using System.Collections.Generic;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Protège le corps d'un patch Harmony pour qu'il ne puisse jamais casser le jeu.
    ///
    /// Une exception levée dans un Postfix ne reste PAS dans le mod : elle remonte dans la méthode
    /// du jeu qu'on a greffée. Une erreur en annonçant une quête rendue casserait donc la remise de
    /// la quête elle-même ; une erreur en annonçant des dégâts casserait la gestion des dégâts. Le
    /// mod doit pouvoir échouer sans emporter la partie avec lui — c'est le principe déjà appliqué
    /// à la boucle du mod (voir AccessibilityRunner.SafeTick), qui manquait aux patches.
    ///
    /// La trace n'est écrite qu'une fois par patch : plusieurs de ces méthodes sont appelées des
    /// dizaines de fois par minute, et une erreur durable remplirait le disque en décrivant à
    /// chaque fois le même problème, déjà entièrement décrit par la première ligne.
    /// </summary>
    internal static class PatchGuard
    {
        private static readonly HashSet<string> _reported = new HashSet<string>();

        internal static void Run(string patchName, Action body)
        {
            try
            {
                body();
            }
            catch (Exception e)
            {
                if (_reported.Add(patchName))
                {
                    Plugin.Log?.LogWarning(
                        $"Erreur dans le patch {patchName} : {e}\n" +
                        "Ce patch est désormais silencieux pour le reste de la session ; le jeu, lui, " +
                        "continue normalement.");
                }
            }
        }
    }
}
