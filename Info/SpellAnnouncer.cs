using System.Collections.Generic;
using System.Reflection;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Les quatre sorts équipés.
    ///
    /// Sun Haven donne quatre emplacements de sorts, lancés par les touches dédiées du jeu. Ils
    /// n'apparaissent qu'en icônes dans une barre : sans la vue, on lance des touches en espérant
    /// se souvenir de ce qu'on y avait mis, et on découvre son erreur en plein combat.
    ///
    /// Le mod ne s'occupe ici que de dire ce qui est équipé. Deux cas voisins n'ont pas besoin de
    /// lui : le jeu envoie déjà une notification quand un sort est en recharge ou quand le mana
    /// manque, et le patch générique de notifications les lit.
    /// </summary>
    public static class SpellAnnouncer
    {
        // `SpellUseItem.itemData` est protégé ; c'est pourtant la seule source du nom lisible du
        // sort. Résolu une fois, gardé en cache.
        private static FieldInfo _itemDataField;
        private static bool _resolved;

        public static void AnnounceEquipped()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            var slots = new List<SpellUseItem>();
            try
            {
                slots.Add(player.Spell1);
                slots.Add(player.Spell2);
                slots.Add(player.Spell3);
                slots.Add(player.Spell4);
            }
            catch
            {
                TolkSpeech.Speak("Sorts illisibles pour le moment.", true);
                return;
            }

            var parts = new List<string>();
            int equipped = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                SpellUseItem spell = slots[i];
                if (spell == null)
                {
                    parts.Add($"{i + 1}, vide");
                    continue;
                }

                equipped++;
                string name = NameOf(spell) ?? "sort inconnu";
                bool casting = false;
                try { casting = spell.Casting; } catch { }

                parts.Add(casting ? $"{i + 1}, {name}, en cours d'incantation" : $"{i + 1}, {name}");
            }

            if (equipped == 0)
            {
                TolkSpeech.Speak("Aucun sort équipé.", true);
                return;
            }

            TolkSpeech.Speak("Sorts : " + string.Join(". ", parts) + ".", true);
        }

        private static string NameOf(SpellUseItem spell)
        {
            if (!_resolved)
            {
                _resolved = true;
                _itemDataField = typeof(SpellUseItem).GetField("itemData",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (_itemDataField == null)
                    Plugin.Log?.LogWarning("SpellUseItem.itemData introuvable : les sorts seront annoncés sans nom.");
            }

            if (_itemDataField == null) return null;

            try
            {
                var data = _itemDataField.GetValue(spell) as ItemData;
                string name = data?.UnformattedDisplayName;
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch { return null; }
        }
    }
}
