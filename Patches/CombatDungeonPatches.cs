using HarmonyLib;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Retour vocal pour le donjon de combat (Wish.CombatDungeon) : une succession de salles à
    /// vider de tous leurs ennemis (`enemies[]`, vérifiés chaque frame dans LateUpdate — la
    /// porte s'ouvre dès qu'il n'en reste aucun) pour avancer d'étage en étage
    /// (`CurrentFloor`/`CompletedFloor`, statiques). Jusqu'ici entièrement silencieux au-delà du
    /// combat lui-même déjà couvert par Combat/CombatAnnouncer : rien n'indiquait qu'une salle
    /// était terminée, ni à quel étage on se trouvait — repère important dans un donjon répétitif
    /// où toutes les salles se ressemblent visuellement.
    /// </summary>
    [HarmonyPatch(typeof(CombatDungeon), "Awake")]
    public static class CombatDungeonAwakePatch
    {
        private static void Postfix()
        {
            // CurrentFloor n'est mis à jour que si `setFloor` (privé) est vrai pour CETTE salle
            // — pas moyen de le vérifier depuis ici sans réflexion, mais annoncer le numéro
            // d'étage actuel reste correct dans tous les cas (il ne change simplement pas pour
            // les salles qui ne le redéfinissent pas).
            TolkSpeech.Speak($"Donjon de combat, étage {CombatDungeon.CurrentFloor}.", interrupt: false);
        }
    }

    [HarmonyPatch(typeof(CombatDungeon), nameof(CombatDungeon.OpenGate))]
    public static class CombatDungeonGateOpenPatch
    {
        private static void Postfix()
        {
            TolkSpeech.Speak("Salle nettoyée, porte ouverte.", interrupt: false);
        }
    }
}
