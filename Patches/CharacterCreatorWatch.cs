using HarmonyLib;
using Wish;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// L'écran de création de personnage prévient quand il arrive et quand il part.
    ///
    /// POURQUOI PAS SIMPLEMENT DEMANDER. `SingletonBehaviour&lt;T&gt;.Instance` ressemble à une
    /// lecture de champ, et l'est — tant que l'objet existe. Absent, elle relance
    /// `FindObjectOfType`, un balayage de toute la scène, et recommence à chaque appel puisqu'elle
    /// ne trouve jamais rien à retenir. Deux modules guettaient ainsi cet écran ; le chronomètre
    /// intégré a chiffré un seul de ces balayages à vingt-trois millisecondes, alors qu'une image
    /// entière en dure seize.
    ///
    /// Espacer les demandes a fait passer le jeu de vingt-cinq à cent trente images par seconde,
    /// mais espacer, c'est encore payer — un peu moins souvent. La vraie réponse est de ne plus
    /// demander du tout : le jeu appelle `OnEnable` quand l'écran s'ouvre et `OnDisable` quand il
    /// se ferme, et il suffit d'écouter. Coût permanent : rien.
    ///
    /// C'est la règle générale à retenir pour ce mod : guetter une apparition en interrogeant la
    /// scène coûte à chaque image de la partie ; se faire prévenir ne coûte qu'aux deux instants
    /// où cela change.
    /// </summary>
    public static class CharacterCreatorWatch
    {
        /// <summary>L'écran s'il est ouvert, null sinon. Toujours à jour, jamais cherché.</summary>
        public static NewCharacterCreator Current { get; private set; }

        [HarmonyPatch(typeof(NewCharacterCreator), "OnEnable")]
        public static class OpenPatch
        {
            private static void Postfix(NewCharacterCreator __instance) =>
                PatchGuard.Run("CharacterCreatorOpen", () => Current = __instance);
        }

        [HarmonyPatch(typeof(NewCharacterCreator), "OnDisable")]
        public static class ClosePatch
        {
            private static void Postfix(NewCharacterCreator __instance) =>
                PatchGuard.Run("CharacterCreatorClose", () =>
                {
                    // On ne relâche que SI c'est bien celui qu'on tenait : deux écrans successifs
                    // peuvent se chevaucher d'une image, et effacer aveuglément ferait oublier le
                    // nouveau qui vient tout juste de s'annoncer.
                    if (Current == __instance) Current = null;
                });
        }
    }
}
