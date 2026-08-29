using UnityEngine;
using Wish;
using SunHavenAccess.Config;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Cursor
{
    /// <summary>
    /// Avancer d'UNE case par pression, au lieu de glisser en continu.
    ///
    /// POURQUOI. Sun Haven déplace le personnage tant qu'on tient la touche. À l'œil, on s'arrête
    /// là où l'on voit qu'il faut s'arrêter ; sans la vue, on dépasse, on revient, on redépasse.
    /// Se placer exactement sur une case — pour labourer la bonne, franchir une porte étroite,
    /// longer une clôture — devient un jeu de patience au lieu d'être une évidence. C'est le mode
    /// que stardew-access appelle le déplacement à la grille, et il manquait ici.
    ///
    /// COMMENT. Une flèche, un pas, une case, et l'annonce de ce sur quoi on arrive. On ne
    /// téléporte rien : le pas passe par le MÊME cheminement que le reste du mod, celui qui connaît
    /// déjà les obstacles, les collisions et le relief isométrique. Une case bloquée refuse le pas
    /// et le dit, exactement comme un mur refuse un trajet.
    ///
    /// QUI A LA MAIN SUR LES FLÈCHES. Trois choses peuvent les vouloir, et l'ordre est net : un
    /// menu d'abord, toujours ; le curseur de case libre ensuite, puisqu'on l'a activé exprès pour
    /// explorer sans bouger ; ce mode en dernier. Il ne prend donc jamais les flèches à quelque
    /// chose qui les utilisait déjà — c'est ce qui a cassé la navigation deux fois par le passé.
    ///
    /// COÛT EN FOND : nul. Tant que le mode est éteint, `Tick` s'arrête à la première ligne.
    /// </summary>
    internal static class StepMovement
    {
        private static bool _active;

        /// <summary>
        /// Le pas précédent doit être fini avant d'en lancer un autre. Sans cela, maintenir une
        /// flèche enchaînerait les demandes de cheminement sur un personnage déjà en route, et l'on
        /// retomberait sur le glissement continu qu'on cherche justement à éviter.
        /// </summary>
        private static bool Busy => PathingController.IsPathing;

        internal static bool Active => _active;

        internal static void Toggle()
        {
            _active = !_active;
            TolkSpeech.Speak(Localization.Language.T(
                _active
                    ? "Déplacement case par case activé. Une flèche, un pas."
                    : "Déplacement case par case désactivé.",
                _active
                    ? "Tile by tile movement on. One arrow, one step."
                    : "Tile by tile movement off."), true);
        }

        internal static void Tick()
        {
            if (!_active) return;

            Player player = Player.Instance;
            if (player == null) return;

            // Un menu a la priorité absolue sur les flèches, et le curseur de case libre juste
            // après : on ne prend jamais les flèches à qui s'en sert déjà.
            if (Menus.VoiceMenus.AnyOpen || Menus.ZoneNavigator.IsActive()) return;
            if (FreeTileCursor.Active) return;

            if (Busy) return;

            if (UnityEngine.Input.GetKeyDown(KeyCode.UpArrow)) Step(player, 0, 1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow)) Step(player, 0, -1);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.LeftArrow)) Step(player, -1, 0);
            else if (UnityEngine.Input.GetKeyDown(KeyCode.RightArrow)) Step(player, 1, 0);
        }

        /// <summary>
        /// Un pas vers la case voisine. On annonce ce sur quoi on ARRIVE, pas la direction prise :
        /// la direction, on vient de la choisir ; ce qu'il y a sous les pieds, non.
        /// </summary>
        private static void Step(Player player, int dx, int dy)
        {
            var target = new Vector2Int(player.Position.x + dx, player.Position.y + dy);
            Vector3 world = TileGeometry.TileToWorld(target);

            // Le cheminement refuse de lui-même une case bloquée, et le dit. On lui passe donc la
            // décision plutôt que de tester nous-mêmes : deux règles de collision qui se
            // contrediraient un jour valent moins qu'une seule qui fait autorité.
            PathingController.TravelTo(world, Localization.Language.T("la case voisine", "the next tile"));
        }
    }
}
