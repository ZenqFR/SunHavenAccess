using System.Linq;
using Rewired;
using UnityEngine;

namespace SunHavenAccess.Input
{
    /// <summary>
    /// Quelle touche le joueur a-t-il pour telle action DU JEU ?
    ///
    /// Le mod sait dire ses propres touches ; il ne savait pas dire celles du jeu. Or beaucoup de
    /// choses ne s'ouvrent qu'avec elles — une porte de boutique, un coffre, un dialogue — et
    /// annoncer « appuyez sur la touche d'interaction » sans dire laquelle ne sert à rien quand on
    /// ne voit pas l'infobulle qui l'affiche.
    ///
    /// On lit la configuration RÉELLE, celle du joueur, options comprises : une touche changée dans
    /// les paramètres du jeu sera annoncée telle qu'elle est. Une table écrite à la main aurait
    /// menti dès la première personnalisation.
    ///
    /// Le résultat est retenu : ces associations ne changent qu'en passant par les options, et
    /// parcourir toutes les cartes de touches à chaque annonce serait absurde.
    /// </summary>
    internal static class GameKeys
    {
        private static readonly System.Collections.Generic.Dictionary<Wish.Button, string> _cache =
            new System.Collections.Generic.Dictionary<Wish.Button, string>();

        /// <summary>Le nom prononçable de la touche liée à cette action, ou null si on l'ignore.</summary>
        internal static string NameFor(Wish.Button button)
        {
            if (_cache.TryGetValue(button, out string known)) return known;

            string name = Lookup(button);

            // On ne retient que les réponses utiles : tant que Rewired n'est pas prêt, une réponse
            // vide ne doit pas s'installer définitivement.
            if (!string.IsNullOrEmpty(name)) _cache[button] = name;

            return name;
        }

        private static string Lookup(Wish.Button button)
        {
            try
            {
                if (!ReInput.isReady) return null;

                int wanted = (int)button;

                foreach (Rewired.Player player in ReInput.players.AllPlayers)
                {
                    if (player == null) continue;

                    foreach (KeyboardMap map in player.controllers.maps.GetAllMaps<KeyboardMap>())
                    {
                        ActionElementMap element = map?.AllMaps?
                            .FirstOrDefault(m => m != null && m.actionId == wanted && m.keyCode != KeyCode.None);

                        if (element != null) return Localization.Strings.KeyName(element.keyCode);
                    }
                }
            }
            catch { }

            return null;
        }
    }
}
