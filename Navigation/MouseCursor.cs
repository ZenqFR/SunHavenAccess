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
        public static void Tick()
        {
            if (!_enabled) return;
            Player player = Player.Instance;
            if (player == null) return;

            Vector2Int tile = player.Position;
            Direction dir = player.facingDirection;
            if (_hasLast && tile == _lastTile && dir == _lastDir) return;
            _lastTile = tile;
            _lastDir = dir;
            _hasLast = true;
            UpdatePosition();
        }

        private static void UpdatePosition()
        {
            try
            {
                Player player = Player.Instance;
                Camera cam = player != null ? player.Camera : null;
                if (player == null || cam == null) return;

                Vector3 frontWorld = player.transform.position + Utilities.OffsetFromDirection(player.facingDirection);
                Vector3 screen = cam.WorldToScreenPoint(frontWorld);

                IntPtr hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                Point pt = new Point
                {
                    X = Mathf.RoundToInt(screen.x),
                    Y = Mathf.RoundToInt(Screen.height - screen.y) // Unity: Y vers le haut ; Windows: Y vers le bas
                };
                if (hWnd != IntPtr.Zero) ClientToScreen(hWnd, ref pt);
                SetCursorPos(pt.X, pt.Y);
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("MouseCursor.UpdatePosition a échoué : " + e.Message);
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
