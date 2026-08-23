using UnityEngine;
using Wish;

namespace SunHavenAccess.Localization
{
    /// <summary>
    /// Gabarits de phrases centralisés en français. Regrouper les chaînes ici permet
    /// d'ajuster le style ou d'ajouter une autre langue plus tard sans toucher au reste du mod.
    /// </summary>
    public static class Strings
    {
        public static string DirectionName(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return "nord";
                case Direction.South: return "sud";
                case Direction.East: return "est";
                case Direction.West: return "ouest";
                default: return dir.ToString();
            }
        }

        private static readonly string[] Compass8 =
        {
            "est", "nord-est", "nord", "nord-ouest", "ouest", "sud-ouest", "sud", "sud-est"
        };

        /// <summary>
        /// Direction approximative (8 secteurs) d'un delta de position monde vers une cible,
        /// en tenant compte de la déformation isométrique du monde (facteur racine de 2 en Y).
        /// </summary>
        public static string BearingName(Vector3 delta)
        {
            Vector2 v = new Vector2(delta.x, delta.y / 1.4142135f);
            if (v.sqrMagnitude < 0.01f) return "sur vous";
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            int idx = Mathf.RoundToInt(angle / 45f) % 8;
            return Compass8[idx];
        }

        /// <summary>Nom lisible d'une touche pour l'annoncer à voix haute (l'aide, etc.).</summary>
        public static string KeyName(KeyCode key)
        {
            string s = key.ToString();
            if (s.StartsWith("Keypad"))
            {
                string rest = s.Substring(6);
                return rest == "Enter" ? "entrée du pavé numérique" : "pavé numérique " + rest;
            }
            return s;
        }
    }
}
