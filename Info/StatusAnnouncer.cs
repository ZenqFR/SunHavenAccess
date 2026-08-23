using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>Touche dédiée : annonce la santé et le mana actuels, à la demande.</summary>
    public static class StatusAnnouncer
    {
        public static void Announce()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            int health = Mathf.CeilToInt(Mathf.Max(0f, player.Health));
            int maxHealth = Mathf.CeilToInt(player.MaxHealth);
            int mana = Mathf.CeilToInt(Mathf.Max(0f, player.Mana));
            int maxMana = Mathf.CeilToInt(player.MaxMana);

            TolkSpeech.Speak($"Santé : {health} sur {maxHealth}. Mana : {mana} sur {maxMana}.", true);
        }
    }
}
