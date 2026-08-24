using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SunHavenAccess.Speech
{
    /// <summary>
    /// Petits retours sonores non verbaux de l'interface — actuellement le "bip de bord",
    /// joué quand on bute sur le bord d'une zone de navigation (fin de ligne d'inventaire,
    /// dernier onglet...). Un son court est bien plus rapide qu'une phrase pour ça : il ne coupe
    /// pas l'annonce de l'élément en cours et n'allonge pas un parcours répétitif.
    ///
    /// `kernel32.Beep` plutôt qu'un son système Windows (`winmm.PlaySound`, cf. TestTone.cs) :
    /// fréquence et durée contrôlées, donc un son délibérément grave et bref, impossible à
    /// confondre avec les sons du jeu, et surtout audible même si les sons système de Windows
    /// sont désactivés. `Beep` étant BLOQUANTE pour toute sa durée, elle est lancée sur le
    /// ThreadPool (jamais sur le thread du jeu, qui gèlerait) — un bip de bord est un évènement
    /// rare et ponctuel, pas besoin du thread dédié qu'utilise Speech/FishingToneCue.cs pour son
    /// bip continu.
    /// </summary>
    public static class UiSound
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Beep(uint frequency, uint duration);

        private const uint EdgeFrequencyHz = 220;
        private const uint EdgeDurationMs = 45;

        /// <summary>Désactivable via la config (voir ModConfig.EdgeSound).</summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// Bip court signalant "vous êtes au bord, ce déplacement n'a rien changé". Anti-spam :
        /// une pression maintenue sur une flèche déclenche une répétition rapide, inutile d'en
        /// empiler plusieurs.
        /// </summary>
        private static DateTime _lastEdgeBeep = DateTime.MinValue;
        private static readonly TimeSpan MinInterval = TimeSpan.FromMilliseconds(120);

        public static void EdgeBump()
        {
            if (!Enabled) return;

            DateTime now = DateTime.UtcNow;
            if (now - _lastEdgeBeep < MinInterval) return;
            _lastEdgeBeep = now;

            try
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try { Beep(EdgeFrequencyHz, EdgeDurationMs); }
                    catch { /* pas de son disponible : jamais bloquant pour la navigation */ }
                });
            }
            catch
            {
                // ThreadPool saturé ou indisponible : on abandonne silencieusement, le bip est
                // un confort, jamais une information indispensable (l'élément reste annoncé).
            }
        }
    }
}
