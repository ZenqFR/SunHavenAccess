using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Annonce l'entrée/sortie de combat (`Wish.Player.OnEnterCombat`/`OnExitCombat`, deux
    /// UnityAction STATIQUES — abonnement une seule fois au démarrage, comme les évènements
    /// agricoles de FarmingAnnouncer). Le jeu calcule déjà lui-même ces transitions (fenêtre de
    /// quelques secondes autour des coups donnés/reçus, voir Player.CheckForInCombat en
    /// décompilation) : on se contente de les relayer, repère utile pour un joueur aveugle qui
    /// ne peut pas voir la barre de vie d'un ennemi apparaître à l'écran.
    /// </summary>
    public static class CombatStateAnnouncer
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;

            Player.OnEnterCombat += () => TolkSpeech.Speak("En combat.", interrupt: false);
            Player.OnExitCombat += () => TolkSpeech.Speak("Combat terminé.", interrupt: false);
        }
    }
}
