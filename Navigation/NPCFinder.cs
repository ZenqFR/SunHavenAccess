using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Localization;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Repère les PNJ proches (Wish.NPCManager._npcsList), filtrés à la carte actuellement
    /// chargée et dans un rayon raisonnable, triés par distance. Une touche dédiée fait défiler
    /// la liste en annonçant nom, direction approximative et distance en cases.
    /// </summary>
    public static class NPCFinder
    {
        private const float MaxRadius = 25f;

        private static int _index = -1;
        private static List<NPCAI> _cached = new List<NPCAI>();

        public static void AnnounceNext()
        {
            Player player = Player.Instance;
            NPCManager mgr = NPCManager.Instance;
            if (player == null || mgr == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            RefreshList(player, mgr);
            if (_cached.Count == 0)
            {
                TolkSpeech.Speak("Aucun personnage à proximité.", true);
                _index = -1;
                return;
            }

            _index = (_index + 1) % _cached.Count;
            Announce(player, _cached[_index], _index + 1, _cached.Count);
        }

        private static void RefreshList(Player player, NPCManager mgr)
        {
            // Le joueur et les PNJ ne vivent pas forcément dans la même scène Unity (carte
            // chargée en scène additive séparée) : comparer gameObject.scene.name des deux ne
            // trouvait donc jamais rien. Le jeu compare le champ NPCAI.Scene à
            // ScenePortalManager.ActiveSceneName (voir NPCManager.ManageGraphicsForNPC) — on fait
            // pareil.
            string activeScene = ScenePortalManager.ActiveSceneName;
            Vector3 ppos = player.transform.position;

            _cached = mgr._npcsList
                .Where(n => n != null && n.gameObject != null && n.Scene == activeScene)
                .Where(n => Vector2.Distance(ppos, n.transform.position) <= MaxRadius)
                .OrderBy(n => Vector2.Distance(ppos, n.transform.position))
                .ToList();
        }

        private static void Announce(Player player, NPCAI npc, int position, int total)
        {
            Vector3 delta = npc.transform.position - player.transform.position;
            float distanceTiles = Mathf.Round(new Vector2(delta.x, delta.y / 1.4142135f).magnitude);
            string bearing = Strings.BearingName(delta);
            string name = npc.LocalizedActualNPCName;
            string plural = distanceTiles > 1 ? "s" : "";

            TolkSpeech.Speak(
                $"{name}, {bearing}, {distanceTiles} case{plural}. Personnage {position} sur {total}.",
                true);
        }
    }
}
