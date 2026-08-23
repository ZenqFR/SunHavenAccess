using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Patches
{
    /// <summary>
    /// Wish.NotificationStack.SendNotification est le point d'entrée GÉNÉRIQUE des petites
    /// notifications visuelles éphémères du jeu (bulle en haut à gauche, quelques secondes) —
    /// trouvé en décompilant plusieurs systèmes sans rapport les uns avec les autres
    /// (Wish.ScenePortalSpot pour "cette boutique est fermée", entre autres) qui appellent tous
    /// la MÊME méthode avec du texte déjà résolu/localisé. Rien dans le mod ne les lisait
    /// jusqu'ici : un seul patch Harmony ici couvre donc potentiellement toutes sortes de
    /// messages jamais vus autrement (boutique fermée, action impossible, objet reçu...), sans
    /// avoir à chercher et corriger chaque système séparément.
    ///
    /// Anti-spam : le jeu lui-même évite de recréer une bulle visuelle identique déjà affichée
    /// (il prolonge juste son minuteur) — on reproduit l'esprit de ce comportement avec un
    /// anti-rebond simple (même texte, moins de 3 secondes) plutôt que de fouiller son
    /// dictionnaire interne privé par réflexion.
    /// </summary>
    [HarmonyPatch(typeof(NotificationStack), "SendNotification")]
    public static class NotificationPatch
    {
        private static string _lastText = "";
        private static float _lastTime = -10f;

        private static void Postfix(string text, bool error)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            string clean = TextUtil.Clean(text);
            if (string.IsNullOrWhiteSpace(clean)) return;

            float now = Time.time;
            if (clean == _lastText && now - _lastTime < 3f) return;
            _lastText = clean;
            _lastTime = now;

            // Une erreur ("action impossible", boutique fermée...) est en général la réaction
            // directe à ce que le joueur vient de tenter : plus utile de l'entendre tout de
            // suite, quitte à couper une annonce de fond, qu'en attendant son tour derrière
            // d'autres annonces non prioritaires.
            TolkSpeech.Speak(clean, interrupt: error);
        }
    }
}
