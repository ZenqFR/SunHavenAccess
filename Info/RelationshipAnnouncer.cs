using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Relations avec les PNJ romançables (`Wish.GameSave.CurrentCharacter.Relationships`, un
    /// `Dictionary&lt;string, float&gt;` PUBLIC : nom technique du PNJ → points de relation).
    /// Le menu natif du jeu (`Wish.Relationships`/`RelationshipPanel`) affiche ça sous forme de
    /// cœurs remplis visuellement — pas de texte natif équivalent à lire. Cette touche calcule
    /// directement l'équivalent en cœurs à partir des points, plutôt que de dépendre de cet
    /// écran.
    /// Plafonds confirmés en décompilation (NPCAI.AddRelationship) : 50 points tant qu'on n'est
    /// pas en couple, 75 en couple, 100 marié(e) — 5 points par cœur, donc 10/15/20 cœurs max
    /// selon le statut (NPCAI.IsDatingPlayer/IsMarriedToPlayer, publiques).
    /// </summary>
    public static class RelationshipAnnouncer
    {
        private const float PointsPerHeart = 5f;

        public static void AnnounceAll()
        {
            NPCManager mgr = NPCManager.Instance;
            if (mgr == null || SingletonBehaviour<GameSave>.Instance == null || GameSave.CurrentCharacter == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            Dictionary<string, float> relationships = GameSave.CurrentCharacter.Relationships;
            if (relationships == null || relationships.Count == 0)
            {
                TolkSpeech.Speak("Aucune relation nouée pour l'instant.", true);
                return;
            }

            var entries = new List<(NPCAI npc, float points)>();
            foreach (KeyValuePair<string, float> kv in relationships)
            {
                NPCAI npc = mgr.GetNPC(kv.Key);
                if (npc == null || !npc.Romanceable) continue;
                entries.Add((npc, kv.Value));
            }

            if (entries.Count == 0)
            {
                TolkSpeech.Speak("Aucune relation nouée pour l'instant.", true);
                return;
            }

            // Relation la plus investie en premier, comme le scanner trie du plus proche au
            // plus loin : ce qui compte le plus arrive en premier dans l'annonce.
            entries.Sort((a, b) => b.points.CompareTo(a.points));

            var parts = new List<string>();
            foreach (var (npc, points) in entries)
            {
                bool married = npc.IsMarriedToPlayer();
                bool dating = !married && npc.IsDatingPlayer();
                float max = married ? 100f : (dating ? 75f : 50f);

                int hearts = Mathf.FloorToInt(Mathf.Clamp(points, 0f, max) / PointsPerHeart);
                int maxHearts = Mathf.FloorToInt(max / PointsPerHeart);

                string name = TextUtil.Clean(npc.LocalizedActualNPCName);
                string status = married ? ", marié(e)" : (dating ? ", en couple" : "");
                parts.Add($"{name}, {hearts} sur {maxHearts} cœurs{status}.");
            }

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }
    }
}
