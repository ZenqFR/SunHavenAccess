using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Déplace le vrai curseur souris Windows pour qu'il pointe toujours vers la case devant
    /// le joueur (comme le curseur directionnel de stardew-access) : la souris "regarde" dans
    /// la direction du personnage. Beaucoup d'actions de Sun Haven (arroser, labourer,
    /// récolter, miner, attaquer...) se déclenchent par un clic à la position de la souris
    /// plutôt que par une touche d'interaction dédiée ; ce système permet de les utiliser sans
    /// avoir besoin de voir l'écran pour viser.
    /// </summary>
    public static class MouseCursor
    {
        private static bool _enabled;
        private static Vector2Int _lastTile;
        private static Direction _lastDir;
        private static bool _hasLast;

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct Point { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

        public static bool Enabled => _enabled;

        public static void Toggle()
        {
            _enabled = !_enabled;
            _hasLast = false;
            TolkSpeech.Speak(_enabled
                ? "Souris directionnelle activée : elle pointe désormais vers la case devant vous."
                : "Souris directionnelle désactivée, vous pouvez la déplacer librement.", true);
            if (_enabled) UpdatePosition();
        }

        /// <summary>Appelé chaque frame : replace le curseur si la case visée a changé.</summary>
        /// <summary>
        /// Cède le pointeur à un autre module tant que c'est à true.
        ///
        /// Le curseur souris directionnel repointe sur la case devant le personnage dès que
        /// celui-ci bouge ou se tourne. Pendant un placement piloté au curseur libre, ce
        /// repointage arracherait la visée à chaque pas — deux modules écrivant la même position
        /// Windows, le dernier gagnant de façon imprévisible. Un seul propriétaire à la fois.
        /// </summary>
        public static bool ExternalControl { get; set; }

        public static void Tick()
        {
            if (!_enabled || ExternalControl) return;
            Player player = Player.Instance;
            if (player == null) return;

            // LA SOURIS EST VERROUILLÉE, PAS SEULEMENT REPLACÉE.
            //
            // On ne la remettait devant le personnage qu'au changement de case ou de direction.
            // Entre-temps, le moindre frôlement du bureau — ou un tapis tactile qu'on effleure sans
            // le vouloir — l'emmenait ailleurs et elle y restait. Elle survolait alors un élément
            // d'interface, et Unity donne le focus clavier à ce qui est survolé : la navigation
            // repartait de là où traînait la souris, pas de là où on en était. Signalé en jeu, au
            // menu principal comme dans l'inventaire.
            //
            // Tant que J est actif, la position est donc MAINTENUE à chaque image. Bouger la souris
            // physiquement n'a plus d'effet ; J reste la seule façon de la libérer.
            Vector3 target = player.transform.position + Utilities.OffsetFromDirection(player.facingDirection);

            Vector2Int tile = player.Position;
            Direction dir = player.facingDirection;
            bool moved = !_hasLast || tile != _lastTile || dir != _lastDir;
            _lastTile = tile;
            _lastDir = dir;
            _hasLast = true;

            // On ne rappelle le système que si la souris a VRAIMENT dérivé : la repositionner
            // soixante fois par seconde sans raison est un appel système gratuit, et cela
            // empêcherait aussi tout déplacement légitime d'un frame à l'autre.
            if (moved || HasDrifted(target)) PointAt(target);
        }

        /// <summary>
        /// La souris s'est-elle éloignée de l'endroit où on la veut ?
        ///
        /// Quelques pixels de tolérance : la conversion monde → écran → pixels arrondit, et exiger
        /// l'exactitude ferait replacer le curseur en boucle pour un écart qui n'existe pas.
        /// </summary>
        private static bool HasDrifted(Vector3 target)
        {
            try
            {
                Player player = Player.Instance;
                Camera cam = player != null ? player.Camera : null;
                if (cam == null) return false;

                Vector3 wanted = cam.WorldToScreenPoint(target);
                Vector3 actual = UnityEngine.Input.mousePosition;
                return Mathf.Abs(wanted.x - actual.x) > 3f || Mathf.Abs(wanted.y - actual.y) > 3f;
            }
            catch { return false; }
        }

        private static void UpdatePosition()
        {
            Player player = Player.Instance;
            if (player == null) return;
            PointAt(player.transform.position + Utilities.OffsetFromDirection(player.facingDirection));
        }

        /// <summary>
        /// Place le curseur Windows sur une position MONDE quelconque. Extrait de UpdatePosition,
        /// qui était codée en dur sur la case devant le personnage : le curseur de case libre
        /// (Cursor/FreeTileCursor.cs) a besoin de viser une case arbitraire pour permettre d'agir
        /// à distance. Seule l'origine change — la chaîne monde → écran → coordonnées Windows est
        /// identique dans les deux cas.
        /// </summary>
        /// <summary>
        /// La fenêtre du jeu, demandée UNE fois.
        ///
        /// `Process.GetCurrentProcess()` alloue un objet et interroge le système à chaque appel.
        /// C'était supportable tant qu'on ne replaçait la souris qu'au changement de case ; ça ne
        /// l'est plus maintenant qu'on la maintient en place à chaque image. La fenêtre du jeu ne
        /// change pas en cours de partie.
        /// </summary>
        private static IntPtr _hWnd = IntPtr.Zero;

        private static IntPtr WindowHandle()
        {
            if (_hWnd != IntPtr.Zero) return _hWnd;
            try { _hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle; }
            catch { _hWnd = IntPtr.Zero; }
            return _hWnd;
        }

        public static bool PointAt(Vector3 world)
        {
            try
            {
                Player player = Player.Instance;
                Camera cam = player != null ? player.Camera : null;
                if (cam == null) return false;

                Vector3 screen = cam.WorldToScreenPoint(world);

                IntPtr hWnd = WindowHandle();
                Point pt = new Point
                {
                    X = Mathf.RoundToInt(screen.x),
                    Y = Mathf.RoundToInt(Screen.height - screen.y) // Unity: Y vers le haut ; Windows: Y vers le bas
                };
                if (hWnd != IntPtr.Zero) ClientToScreen(hWnd, ref pt);
                SetCursorPos(pt.X, pt.Y);
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("MouseCursor.PointAt a échoué : " + e.Message);
                return false;
            }
        }

        public static void SimulateLeftClick()
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        public static void SimulateRightClick()
        {
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
        }
    }
}
