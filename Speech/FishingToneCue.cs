using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace SunHavenAccess.Speech
{
    /// <summary>
    /// Repère audio CONTINU pour le mini-jeu de pêche (Wish.Bobber) : le vrai point faible
    /// d'accessibilité identifié plus tôt ("viser la jauge à l'oreille seule n'est pas encore
    /// possible"). Décompilation de Wish.Bobber : la jauge (`miniGameSlider.value`, 0 à 1)
    /// oscille TOUTE SEULE (aller-retour automatique, tween Yoyo) — le joueur n'a qu'à appuyer
    /// au bon MOMENT pendant qu'elle traverse la zone gagnante (`winMin`/`winMax`, privés). Pas
    /// besoin de suivre deux éléments mobiles indépendants comme dans Stardew Valley : un seul
    /// bip dont la hauteur varie avec la distance à la zone gagnante suffit à viser au son.
    ///
    /// Génère le bip via l'API Windows `Beep` (kernel32.dll) — bloquante pendant sa durée, donc
    /// exécutée sur un thread dédié séparé du thread principal d'Unity (qui gèlerait sinon le
    /// jeu entier à chaque bip). Aucune dépendance audio Unity supplémentaire nécessaire (le
    /// mod ne référence pas UnityEngine.AudioModule).
    /// </summary>
    public static class FishingToneCue
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool Beep(uint frequency, uint duration);

        private const int BeepDurationMs = 70;
        private const int GapMs = 15;
        private const int FarFrequency = 280;
        private const int NearFrequency = 900;
        private const int InZoneFrequency = 1400;

        private static volatile bool _active;
        private static volatile bool _inZone;
        private static float _normalizedDistance = 1f; // 0 = dans la zone gagnante, 1 = aussi loin que possible
        private static Thread _thread;

        /// <summary>Touche dédiée (défaut K) : coupe/rétablit le bip sans quitter la pêche.</summary>
        public static bool Enabled { get; set; } = true;

        public static void ToggleEnabled()
        {
            Enabled = !Enabled;
            if (!Enabled) Stop();
            TolkSpeech.Speak(Enabled ? "Bip de visée en pêche activé." : "Bip de visée en pêche désactivé.", true);
        }

        /// <summary>À appeler chaque frame pendant qu'un mini-jeu de pêche est en cours.</summary>
        public static void UpdateTarget(float sliderValue, float winMin, float winMax)
        {
            if (!Enabled) return;
            if (!_active) Start();

            float center = (winMin + winMax) / 2f;
            float halfWidth = Mathf.Abs(winMax - winMin) / 2f;
            float distanceFromCenter = Mathf.Abs(sliderValue - center);

            _inZone = distanceFromCenter <= halfWidth;

            // Distance normalisée au-delà du bord de la zone gagnante, rapportée à la plus
            // grande distance possible depuis ce bord jusqu'à une extrémité de la jauge (0 ou
            // 1) — pour que le bip reste repérable même très loin de la cible, quelle que soit
            // la position de la zone sur la jauge.
            float maxPossibleDistance = Mathf.Max(center, 1f - center);
            float distanceBeyondZone = Mathf.Max(0f, distanceFromCenter - halfWidth);
            float range = Mathf.Max(0.0001f, maxPossibleDistance - halfWidth);
            _normalizedDistance = _inZone ? 0f : Mathf.Clamp01(distanceBeyondZone / range);
        }

        public static void Start()
        {
            if (_active) return;
            _active = true;
            _thread = new Thread(BeepLoop) { IsBackground = true, Name = "SunHavenAccess-FishingTone" };
            _thread.Start();
        }

        public static void Stop()
        {
            _active = false;
        }

        private static void BeepLoop()
        {
            while (_active)
            {
                bool inZone = _inZone;
                float distance = _normalizedDistance;
                uint frequency = inZone
                    ? (uint)InZoneFrequency
                    : (uint)(FarFrequency + (NearFrequency - FarFrequency) * (1f - distance));

                try
                {
                    Beep(frequency, BeepDurationMs);
                    // Un deuxième bip plus aigu, dos à dos, rend la zone gagnante nettement
                    // reconnaissable au son plutôt qu'un simple bip un peu plus haut que les autres.
                    if (inZone) Beep((uint)(InZoneFrequency + 300), BeepDurationMs);
                }
                catch
                {
                    // Best-effort : un souci ponctuel de l'API Beep ne doit jamais planter le thread.
                }

                Thread.Sleep(GapMs);
            }
        }
    }
}
