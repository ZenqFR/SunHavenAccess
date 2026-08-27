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
        // Les directions et les noms de touches sont les seuls mots de ce fichier qui se retrouvent
        // AU MILIEU d'une phrase construite ailleurs — « Marguerite, à l'est, 12 mètres ». Ils
        // échappent donc à la traduction par phrase entière de Translator, qui ne reconnaît que
        // des phrases complètes, et doivent être traduits ici, à la source.
        public static string DirectionName(Direction dir)
        {
            bool en = Language.IsEnglish;
            switch (dir)
            {
                case Direction.North: return en ? "north" : "nord";
                case Direction.South: return en ? "south" : "sud";
                case Direction.East:  return en ? "east"  : "est";
                case Direction.West:  return en ? "west"  : "ouest";
                default: return dir.ToString();
            }
        }

        private static readonly string[] Compass8 =
        {
            "est", "nord-est", "nord", "nord-ouest", "ouest", "sud-ouest", "sud", "sud-est"
        };

        private static readonly string[] Compass8En =
        {
            "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east"
        };

        /// <summary>
        /// Direction approximative (8 secteurs) d'un delta de position monde vers une cible,
        /// en tenant compte de la déformation isométrique du monde (facteur racine de 2 en Y).
        /// </summary>
        public static string BearingName(Vector3 delta)
        {
            Vector2 v = new Vector2(delta.x, delta.y / 1.4142135f);
            if (v.sqrMagnitude < 0.01f) return Language.IsEnglish ? "on you" : "sur vous";
            float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            int idx = Mathf.RoundToInt(angle / 45f) % 8;
            return Language.IsEnglish ? Compass8En[idx] : Compass8[idx];
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

        /// <summary>
        /// Noms de touches en anglais. Beaucoup plus courte que la table française : en anglais,
        /// `KeyCode.ToString()` donne déjà le bon mot pour presque toutes les touches, puisque
        /// c'est de l'anglais. Seules figurent ici celles dont le nom d'énumération se lit mal —
        /// la ponctuation, dont le nom technique n'est pas ce qu'on dit à voix haute.
        /// </summary>
        private static readonly Dictionary<KeyCode, string> EnglishKeyNames = new Dictionary<KeyCode, string>
        {
            { KeyCode.Return, "Enter" },
            { KeyCode.KeypadEnter, "numpad Enter" },
            { KeyCode.Escape, "Escape" },
            { KeyCode.UpArrow, "up arrow" },
            { KeyCode.DownArrow, "down arrow" },
            { KeyCode.LeftArrow, "left arrow" },
            { KeyCode.RightArrow, "right arrow" },
            { KeyCode.PageUp, "Page Up" },
            { KeyCode.PageDown, "Page Down" },
            { KeyCode.Comma, "comma" },
            { KeyCode.Period, "period" },
            { KeyCode.Semicolon, "semicolon" },
            { KeyCode.Colon, "colon" },
            { KeyCode.Quote, "apostrophe" },
            { KeyCode.Backslash, "backslash" },
            { KeyCode.Slash, "slash" },
            { KeyCode.Equals, "equals" },
            { KeyCode.Minus, "minus" },
            { KeyCode.Plus, "plus" },
            { KeyCode.LeftBracket, "left bracket" },
            { KeyCode.RightBracket, "right bracket" },
            { KeyCode.BackQuote, "backtick" },
        };

        public static string KeyName(KeyCode key)
        {
            bool en = Language.IsEnglish;

            // Un raccourci peut exister sans être assigné : c'est ainsi que sont livrées les
            // actions marginales, disponibles pour qui en veut sans encombrer le clavier de tout
            // le monde. « None » lu tel quel n'aurait aucun sens à l'oral.
            if (key == KeyCode.None) return en ? "unassigned" : "non assignée";

            var table = en ? EnglishKeyNames : FrenchKeyNames;
            if (table.TryGetValue(key, out string named)) return named;

            string s = key.ToString();
            if (s.StartsWith("Keypad"))
            {
                string rest = s.Substring(6);
                if (rest == "Enter") return en ? "numpad Enter" : "entrée du pavé numérique";
                return (en ? "numpad " : "pavé numérique ") + rest;
            }
            return s;
        }
    }
}
