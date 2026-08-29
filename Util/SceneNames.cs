using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wish;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// Le nom lisible d'une zone, en français quand le jeu sait le dire.
    ///
    /// LE PROBLÈME. Les portes du jeu ne connaissent leur destination que sous son nom technique :
    /// `Town10`, `ForestA`, `Leftoffarm`, `Tier1Coop0`. C'est ce qui s'annonçait dans la liste des
    /// sorties et pendant un trajet — illisible, et pire encore à l'oreille qu'à l'œil, puisqu'une
    /// synthèse vocale épelle « Tier one Coop zero » sans qu'on devine le poulailler.
    ///
    /// TROIS SOURCES, DE LA MEILLEURE À LA MOINS BONNE, ET AUCUN TROU AU BOUT.
    ///
    /// 1. **La carte, traduite par le jeu.** Chaque lieu de la carte porte sa clé de traduction :
    ///    `LocalizeText` en rend le nom dans la langue où l'on joue. C'est la seule source
    ///    réellement française, et elle couvre ce qui compte le plus — villes, boutiques, café.
    ///
    /// 2. **Le nom formel du jeu.** `SceneSettings.formalSceneName` existe pour CHAQUE zone, y
    ///    compris celles qui n'apparaissent sur aucune carte : la grange, le poulailler, le champ.
    ///    Il n'est pas traduit, mais il est juste et complet.
    ///
    /// 3. **Le nom technique rendu lisible.** En dernier recours, on sépare les mots collés et on
    ///    retire les numéros de fin : `Leftoffarm` devient « Left of farm ». Ce n'est pas beau,
    ///    mais cela reste prononçable, et cela ne peut jamais manquer.
    ///
    /// Rien n'est écrit en dur : une zone ajoutée par une mise à jour du jeu sera nommée le jour
    /// même, comme pour les objets. Le même principe que partout ailleurs dans ce mod — lire les
    /// données du jeu plutôt que tenir une liste qui vieillira.
    /// </summary>
    internal static class SceneNames
    {
        private static readonly Dictionary<string, string> _resolved =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Nom anglais aplati d'un lieu de carte → son nom traduit. Construit une fois.</summary>
        private static Dictionary<string, string> _fromMap;

        internal static string Translate(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return sceneName;
            if (_resolved.TryGetValue(sceneName, out string known)) return known;

            string best = Resolve(sceneName);

            // On ne retient que si la carte a pu répondre : tant qu'elle n'est pas encore chargée,
            // un nom de repli ne doit pas s'installer définitivement à la place du nom français.
            if (_fromMap != null && _fromMap.Count > 0) _resolved[sceneName] = best;

            return best;
        }

        private static string Resolve(string sceneName)
        {
            string formal = FormalName(sceneName);

            BuildMapNames();
            if (_fromMap != null)
            {
                // Le nom formel d'abord : c'est lui qui ressemble au nom de la carte.
                if (formal != null && _fromMap.TryGetValue(Flatten(formal), out string fromMapTable)) return fromMapTable;
                if (_fromMap.TryGetValue(Flatten(sceneName), out string direct)) return direct;
            }

            // Les zones de la ferme et des alentours n'ont de nom français NULLE PART dans le jeu :
            // ni sur la carte, qui ne les montre pas, ni dans le nom formel, qui reste anglais.
            // On les traduit donc à la main — ce sont les toutes premières qu'on rencontre, et
            // « Wheat Field Revamp » lu par une synthèse vocale donne « the weak champ », signalé
            // en jeu. Une poignée de lignes justes vaut mieux qu'un mécanisme élégant qui se
            // trompe ; le reste continue de passer par les sources du jeu.
            string bare = StripDigits(Flatten(sceneName));
            if (Handwritten.TryGetValue(bare, out string french)) return french;
            if (formal != null && Handwritten.TryGetValue(StripDigits(Flatten(formal)), out string byFormal))
                return byFormal;

            if (!string.IsNullOrWhiteSpace(formal)) return UiNameTranslator.Translate(formal);

            // Ce qui arrive ici n'a de français nulle part : on le note une fois, pour pouvoir
            // compléter la table ci-dessus sur des noms réels plutôt que devinés.
            if (_unknown.Add(sceneName))
                Plugin.Log?.LogInfo($"Nom de zone sans traduction : « {sceneName} » (nom formel : « {formal} »).");

            return UiNameTranslator.Translate(Readable(sceneName));
        }

        /// <summary>
        /// Le nom formel que le jeu attribue à la zone, ou null. `sceneNameDictionary` est public :
        /// aucune réflexion nécessaire, et il couvre toutes les zones, pas seulement celles de la
        /// carte.
        /// </summary>
        private static string FormalName(string sceneName)
        {
            try
            {
                var manager = SceneSettingsManager.Instance;
                if (manager?.sceneNameDictionary == null) return null;

                if (!manager.sceneNameDictionary.TryGetValue(sceneName, out SceneSettings settings) || settings == null)
                    return null;

                string formal = settings.formalSceneName;
                return string.IsNullOrWhiteSpace(formal) ? null : formal.Trim();
            }
            catch { return null; }
        }

        /// <summary>
        /// Relève les noms traduits de TOUS les lieux, des cinq régions, pas seulement celle qu'on
        /// regarde. Un trajet peut viser une région où l'on n'est pas, et la carte les tient toutes
        /// en mémoire de toute façon.
        /// </summary>
        private static void BuildMapNames()
        {
            if (_fromMap != null && _fromMap.Count > 0) return;

            try
            {
                Map map = UIHandler.Instance?.map;
                if (map == null) return;

                var table = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string fieldName in RegionFieldNames)
                {
                    FieldInfo field = typeof(Map).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                    if (field?.GetValue(map) is not List<MapImage> images) continue;

                    foreach (MapImage image in images)
                    {
                        if (image == null || string.IsNullOrWhiteSpace(image.location)) continue;

                        string translated = LocalizeText.TranslateText(image.locationKey, image.location);
                        if (string.IsNullOrWhiteSpace(translated)) continue;

                        table[Flatten(image.location)] = TextUtil.Clean(translated);
                    }
                }

                if (table.Count > 0)
                {
                    _fromMap = table;
                    Plugin.Log?.LogInfo($"Noms de zones : {table.Count} lieux traduits par le jeu.");
                }
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Noms de zones de la carte illisibles : " + e.Message);
            }
        }

        /// <summary>
        /// Les zones que le jeu ne traduit nulle part. Clés aplaties et sans chiffres — le jeu
        /// numérote ses variantes (`Town10`, `Tier1Coop0`) sans que cela change le lieu.
        /// </summary>
        private static readonly Dictionary<string, string> Handwritten =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "town", "Ville" },
                { "foresta", "Forêt" },
                { "forestb", "Forêt" },
                { "leftoffarm", "Ouest de la ferme" },
                { "rightoffarm", "Est de la ferme" },
                { "wheatfieldrevamp", "Champ de blé" },
                { "wheatfield", "Champ de blé" },
                { "beachhuntingground", "Plage" },
                { "tiercoop", "Poulailler" },
                { "tierbarn", "Grange" },
                { "tierhouse", "Maison" },
                { "playerhouse", "Maison" },
                { "farm", "Ferme" },
                { "mines", "Mine" },
                { "mine", "Mine" },
            };

        private static readonly HashSet<string> _unknown =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static string StripDigits(string s) =>
            new string((s ?? string.Empty).Where(c => !char.IsDigit(c)).ToArray());

        private static readonly string[] RegionFieldNames =
        {
            "sunHavenMapImages", "nelvariMapImages", "withergateMapImages",
            "brinestoneMapImages", "greatCityMapImages",
        };

        /// <summary>
        /// `Leftoffarm` → « Left of farm ». On sépare aux majuscules et l'on retire les numéros de
        /// fin, qui ne distinguent que des variantes internes (`Town10`, `Tier1Coop0`) et n'ont
        /// aucun sens pour qui écoute.
        /// </summary>
        private static string Readable(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            string trimmed = raw.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            if (trimmed.Length == 0) trimmed = raw;

            var parts = new List<string>();
            int start = 0;
            for (int i = 1; i < trimmed.Length; i++)
            {
                if (!char.IsUpper(trimmed[i])) continue;
                parts.Add(trimmed.Substring(start, i - start));
                start = i;
            }
            parts.Add(trimmed.Substring(start));

            return string.Join(" ", parts.Where(p => p.Length > 0).ToArray());
        }

        private static string Flatten(string s) =>
            new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
