using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Signale qu'on monte ou qu'on descend de monture.
    ///
    /// À cheval, on se déplace nettement plus vite et les outils ne s'utilisent plus. Un joueur
    /// voyant a le sprite sous les yeux ; sans la vue, le seul indice était que les actions
    /// cessaient de fonctionner — indiscernable d'une panne. Le sifflet servant à la fois à monter
    /// et à descendre, il est aussi facile de se retrouver dans l'état inverse de celui voulu.
    ///
    /// L'état est relu à chaque image plutôt qu'abonné à `Player.onChangeMount` : cet évènement
    /// est un champ d'INSTANCE, et l'instance de joueur est recréée à chaque changement de carte —
    /// un abonnement s'y perdrait silencieusement au premier portail franchi.
    ///
    /// Le refus de monter en intérieur n'est pas traité ici : le jeu envoie déjà une notification,
    /// que le mod lit par son patch générique.
    /// </summary>
    public static class MountAnnouncer
    {
        private static bool _mounted;
        private static bool _known;

        public static void Tick()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                // Chargement, ou retour au menu : on oublie l'état pour ne pas annoncer une
                // fausse transition au retour en jeu.
                _known = false;
                return;
            }

            bool mounted;
            try { mounted = player.Mounted; }
            catch { return; }

            if (!_known)
            {
                // Première lecture de la partie : on enregistre sans rien dire. Annoncer ici
                // reviendrait à commenter un état qui n'a pas changé.
                _known = true;
                _mounted = mounted;
                return;
            }

            if (mounted == _mounted) return;

            _mounted = mounted;
            TolkSpeech.Speak(mounted ? "En selle." : "À pied.", interrupt: false);
        }
    }
}
