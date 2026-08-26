using System.Collections.Generic;
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
        /// <summary>
        /// Noms de touches en français. Sans cette table, `KeyCode.ToString()` renvoie le nom
        /// brut de l'énumération et l'aide vocale annonçait des mots anglais au milieu d'une
        /// phrase française (« Comma », « Backslash », « Quote »...), illisible au lecteur
        /// d'écran. Ne couvre que les touches non alphanumériques : les lettres et chiffres se
        /// lisent déjà correctement tels quels.
        /// </summary>
        private static readonly Dictionary<KeyCode, string> FrenchKeyNames = new Dictionary<KeyCode, string>
        {
            { KeyCode.Return, "Entrée" },
            { KeyCode.KeypadEnter, "entrée du pavé numérique" },
            { KeyCode.Escape, "Échap" },
            { KeyCode.Backspace, "Retour arrière" },
            { KeyCode.Tab, "Tabulation" },
            { KeyCode.Space, "Espace" },
            { KeyCode.Delete, "Suppr" },
            { KeyCode.Insert, "Inser" },
            { KeyCode.Home, "Origine" },
            { KeyCode.End, "Fin" },
            { KeyCode.PageUp, "Page précédente" },
            { KeyCode.PageDown, "Page suivante" },
            { KeyCode.UpArrow, "flèche haut" },
            { KeyCode.DownArrow, "flèche bas" },
            { KeyCode.LeftArrow, "flèche gauche" },
            { KeyCode.RightArrow, "flèche droite" },
            { KeyCode.Comma, "virgule" },
            { KeyCode.Period, "point" },
            { KeyCode.Semicolon, "point-virgule" },
            { KeyCode.Colon, "deux-points" },
            { KeyCode.Quote, "apostrophe" },
            { KeyCode.Backslash, "barre oblique inverse" },
            { KeyCode.Slash, "barre oblique" },
            { KeyCode.Equals, "égal" },
            { KeyCode.Minus, "tiret" },
            { KeyCode.Plus, "plus" },
            { KeyCode.LeftBracket, "crochet ouvrant" },
            { KeyCode.RightBracket, "crochet fermant" },
            { KeyCode.BackQuote, "accent grave" },
        };

        public static string KeyName(KeyCode key)
        {
            // Un raccourci peut exister sans être assigné : c'est ainsi que sont livrées les
            // actions marginales, disponibles pour qui en veut sans encombrer le clavier de tout
            // le monde. « None » lu tel quel n'aurait aucun sens à l'oral.
            if (key == KeyCode.None) return "non assignée";

            if (FrenchKeyNames.TryGetValue(key, out string french)) return french;

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
