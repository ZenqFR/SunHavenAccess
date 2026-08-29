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

            // On rend la main au jeu DÈS l'extinction, pas à la prochaine image : si quoi que ce
            // soit interrompait la boucle entre-temps, le personnage resterait immobile sans que
            // rien n'explique pourquoi. C'est le pire défaut possible pour ce mode.
            if (!_active) RestoreGameMovement();

            TolkSpeech.Speak(Localization.Language.T(
                _active
                    ? "Déplacement case par case activé. Vos touches de déplacement font un pas chacune."
                    : "Déplacement case par case désactivé.",
                _active
                    ? "Tile by tile movement on. Your movement keys now take one step each."
                    : "Tile by tile movement off."), true);
        }

        /// <summary>
        /// Rend au jeu son déplacement continu. Écrit une seule fois, et jamais quand le mode est
        /// éteint : le jeu coupe LUI AUSSI ce drapeau — pendant une cinématique, un dialogue — et
        /// le forcer à vrai en permanence casserait ces moments-là.
        /// </summary>
        private static void RestoreGameMovement()
        {
            try { if (!PlayerInput.AllowMovement) PlayerInput.AllowMovement = true; }
            catch { }
        }

        internal static void Tick()
        {
            if (!_active) return;

            Player player = Player.Instance;
            if (player == null)
            {
                // Hors partie, ce mode n'a plus d'objet — et surtout, il ne doit pas laisser le
                // déplacement du jeu coupé derrière lui.
                _active = false;
                RestoreGameMovement();
                return;
            }

            // ON COUPE LE DÉPLACEMENT CONTINU DU JEU, ET ON MARCHE À SA PLACE.
            //
            // Le mode s'utilisait aux flèches, ce qui obligeait à lâcher les touches de
            // déplacement pour en prendre d'autres — deux façons d'avancer selon le mode, et une
            // gymnastique de plus à retenir. Signalé en jeu : ce doit être ZQSD, comme le reste du
            // temps. Se déplacer se fait avec les touches de déplacement, point.
            //
            // `PlayerInput.AllowMovement` est exactement le drapeau qu'il faut, et
            // `GetButtonDown(..., ignoreAllowInput: true)` lit les touches MÊME quand il est
            // coupé : on garde donc les touches réelles du joueur, quelles qu'elles soient et
            // quelle que soit sa disposition de clavier. Rien n'est supposé sur AZERTY ou QWERTY.
            try { if (PlayerInput.AllowMovement) PlayerInput.AllowMovement = false; }
            catch { }

            // Un menu a la priorité absolue, et le curseur de case libre juste après : on ne prend
            // jamais les touches à qui s'en sert déjà.
            if (Menus.VoiceMenus.AnyOpen || Menus.ZoneNavigator.IsActive()) return;
            if (FreeTileCursor.Active) return;

            if (Busy) return;

            if (Pressed(Button.Up)) Step(player, 0, 1);
            else if (Pressed(Button.Down)) Step(player, 0, -1);
            else if (Pressed(Button.Left)) Step(player, -1, 0);
            else if (Pressed(Button.Right)) Step(player, 1, 0);
        }

        private static bool Pressed(Button button)
        {
            try { return PlayerInput.GetButtonDown(button, ignoreAllowInput: true); }
            catch { return false; }
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
