using System;
using System.Linq;
using System.Reflection;

namespace SunHavenAccess.Localization
{
    /// <summary>
    /// Dans quelle langue le mod parle.
    ///
    /// Il suit le jeu plutôt que Windows : on peut très bien jouer à Sun Haven en anglais sur un
    /// Windows français, et c'est la langue du jeu qui décide de celle des noms d'objets, de PNJ et
    /// de lieux que le mod prononce à côté de ses propres phrases. Les faire diverger donnerait des
    /// annonces à moitié dans chaque langue.
    ///
    /// La langue est lue par réflexion sur le gestionnaire de traduction du jeu (I2 Localization)
    /// plutôt que par une référence à son assembly. Une référence manquante empêcherait le mod
    /// ENTIER de se charger si une mise à jour renommait ou retirait cette bibliothèque ; par
    /// réflexion, l'échec se limite à retomber sur le français, et tout le reste continue.
    /// </summary>
    public static class Language
    {
        /// <summary>
        /// Forçage explicite, renseigné depuis la configuration : "fr", "en", ou vide pour suivre
        /// le jeu. Certains joueurs préfèrent un lecteur d'écran dans une langue et un jeu dans
        /// une autre, et rien dans le jeu ne permet de le deviner.
        /// </summary>
        public static string Override = "";

        private static string _cached;
        private static float _cachedAt = float.NegativeInfinity;

        /// <summary>Vrai si les annonces du mod doivent être en anglais.</summary>
        public static bool IsEnglish => Code() != "fr";

        /// <summary>
        /// Choisit entre deux formulations.
        ///
        /// Pour les MORCEAUX de phrase assemblés sur place — un intitulé suivi d'une valeur, un
        /// mot de liaison entre deux données — que la traduction par phrase entière ne peut pas
        /// atteindre : elle ne voit que le résultat final, où l'intitulé français est noyé au
        /// milieu de valeurs qui, elles, viennent du jeu. Les écrire côte à côte ici garde la
        /// version française sous les yeux au moment d'écrire l'anglaise, et rend visible l'oubli
        /// de l'une des deux.
        /// </summary>
        public static string T(string fr, string en) => IsEnglish ? en : fr;

        /// <summary>
        /// « Intitulé : valeur », avec l'espacement de la langue.
        ///
        /// Le français fait précéder le deux-points d'une espace, l'anglais non. La plupart des
        /// lecteurs d'écran ne prononcent pas le signe, mais certains marquent une pause à
        /// l'espace : « Pièces , 1 240 » s'entend, et sonne faux.
        /// </summary>
        public static string Pair(string label, string value) =>
            IsEnglish ? $"{label}: {value}" : $"{label} : {value}";

        public static string Code()
        {
            if (!string.IsNullOrWhiteSpace(Override)) return Normalize(Override);

            // La langue peut changer en cours de partie, par les options du jeu, mais pas d'une
            // image à l'autre : on relit de temps en temps plutôt qu'à chaque annonce, la
            // réflexion étant bien plus coûteuse qu'une comparaison de chaînes.
            float now = UnityEngine.Time.unscaledTime;
            if (_cached != null && now - _cachedAt < 5f) return _cached;

            _cachedAt = now;
            _cached = Normalize(FromGame());
            return _cached;
        }

        /// <summary>
        /// Ne distingue que le français du reste. Le mod n'existe qu'en deux langues, et un joueur
        /// allemand ou espagnol est bien mieux servi par de l'anglais que par du français.
        /// </summary>
        private static string Normalize(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "fr";
            return code.Trim().ToLowerInvariant().StartsWith("fr") ? "fr" : "en";
        }

        private static Type _manager;
        private static PropertyInfo _property;
        private static bool _looked;

        private static string FromGame()
        {
            try
            {
                if (!_looked)
                {
                    _looked = true;
                    _manager = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => { try { return a.GetType("I2.Loc.LocalizationManager"); } catch { return null; } })
                        .FirstOrDefault(t => t != null);
                    _property = _manager?.GetProperty("CurrentLanguageCode",
                        BindingFlags.Public | BindingFlags.Static);

                    if (_property == null)
                        Plugin.Log?.LogInfo("Langue du jeu introuvable : les annonces resteront en français.");
                }

                return _property?.GetValue(null, null) as string;
            }
            catch { return null; }
        }
    }
}
