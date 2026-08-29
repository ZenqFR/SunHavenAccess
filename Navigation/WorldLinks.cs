using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wish;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Le plan des liaisons entre zones, appris en jouant.
    ///
    /// POURQUOI IL FAUT L'APPRENDRE. Le jeu charge chaque zone séparément et n'expose nulle part
    /// « quelle zone mène à quelle zone ». Il expose autre chose, en revanche : chaque entrée
    /// (`ScenePortalSpot`) déclare la zone vers laquelle elle mène. Une zone visitée livre donc
    /// toutes ses sorties d'un coup — il suffit de les noter. Au fil des visites, ces bouts de
    /// voisinage se recollent en un plan complet.
    ///
    /// CE QUE ÇA PERMET. « Emmène-moi au café » depuis la ferme n'a aucune réponse tant qu'on
    /// raisonne zone par zone : le café n'est pas là, point final. Avec le plan, c'est un chemin —
    /// ferme, ville, café — et le mod peut le parcourir en entier. La contrepartie est honnête et
    /// se dit simplement : le mod ne connaît que ce que vous avez déjà exploré. Un endroit jamais
    /// vu ne peut pas être atteint, et c'est normal.
    ///
    /// CE QUE ÇA NE COÛTE PAS. Rien en fond. On n'observe RIEN à chaque image : le relevé se fait
    /// aux seuls moments où l'on s'en sert déjà — à l'ouverture de la liste des lieux, et à chaque
    /// arrivée dans une zone pendant un trajet. Le plan tient dans un fichier texte à côté de la
    /// configuration, donc il survit aux parties et s'enrichit d'une session à l'autre.
    /// </summary>
    internal static class WorldLinks
    {
        /// <summary>Zone → zones directement atteignables depuis elle.</summary>
        private static readonly Dictionary<string, HashSet<string>> _links =
            new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static bool _loaded;
        private static bool _dirty;

        private static string FilePath =>
            Path.Combine(BepInEx.Paths.ConfigPath, "SunHavenAccess-liaisons.txt");

        internal static string CurrentScene
        {
            get
            {
                try { return ScenePortalManager.ActiveSceneName; }
                catch { return null; }
            }
        }

        /// <summary>
        /// Note les sorties de la zone où l'on se trouve. À appeler aux moments utiles, jamais en
        /// boucle : un relevé est un balayage de scène, bon marché une fois, ruineux à chaque image.
        /// </summary>
        internal static void Learn()
        {
            Load();

            string here = CurrentScene;
            if (string.IsNullOrWhiteSpace(here)) return;

            if (!_links.TryGetValue(here, out HashSet<string> exits))
            {
                exits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                _links[here] = exits;
            }

            foreach (var portal in Scanner.PortalsInScene())
            {
                string destination = Scanner.PortalDestination(portal);
                if (string.IsNullOrWhiteSpace(destination) || destination.Equals(here, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (exits.Add(destination.Trim())) _dirty = true;

                // Une porte se franchit dans les deux sens. Enregistrer le retour évite d'avoir à
                // repasser physiquement par chaque zone pour que le plan devienne utilisable.
                if (!_links.TryGetValue(destination, out HashSet<string> back))
                {
                    back = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _links[destination] = back;
                }
                if (back.Add(here)) _dirty = true;
            }

            Save();
        }

        /// <summary>
        /// La zone connue dont le nom correspond au lieu demandé, ou null si on n'y est jamais allé.
        ///
        /// Les noms ne s'écrivent pas pareil des deux côtés — le jeu appelle « Town10 » ce que la
        /// carte nomme « Ville » — donc on compare sans casse ni ponctuation, et on accepte qu'un
        /// nom soit contenu dans l'autre. Une correspondance ambiguë est refusée : partir vers la
        /// mauvaise zone est pire que ne pas partir.
        /// </summary>
        internal static string FindScene(string wantedName)
        {
            Load();

            string wanted = Flatten(wantedName);
            if (string.IsNullOrEmpty(wanted)) return null;

            var known = _links.Keys
                .Concat(_links.Values.SelectMany(v => v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var exact = known.Where(s => Flatten(s) == wanted).ToList();
            if (exact.Count == 1) return exact[0];

            var partial = known.Where(s =>
            {
                string f = Flatten(s);
                return f.Contains(wanted) || wanted.Contains(f);
            }).ToList();

            return partial.Count == 1 ? partial[0] : null;
        }

        /// <summary>
        /// L'itinéraire de zones à traverser pour aller d'ici à destination, la première étape en
        /// tête et la destination en queue. Null si aucun chemin connu n'y mène.
        ///
        /// Parcours en largeur : il rend le trajet avec le MOINS de zones traversées, ce qui est le
        /// bon critère ici — chaque changement de zone est un temps de chargement, bien plus long
        /// que quelques pas de plus à l'intérieur d'une zone.
        /// </summary>
        internal static List<string> Route(string from, string to)
        {
            Load();

            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to)) return null;
            if (from.Equals(to, StringComparison.OrdinalIgnoreCase)) return new List<string>();

            var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { from };
            var queue = new Queue<string>();
            queue.Enqueue(from);

            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                if (!_links.TryGetValue(current, out HashSet<string> exits)) continue;

                foreach (string next in exits)
                {
                    if (!seen.Add(next)) continue;
                    previous[next] = current;

                    if (next.Equals(to, StringComparison.OrdinalIgnoreCase))
                    {
                        var route = new List<string>();
                        for (string step = to; step != null && !step.Equals(from, StringComparison.OrdinalIgnoreCase);
                             step = previous.TryGetValue(step, out string p) ? p : null)
                        {
                            route.Insert(0, step);
                        }
                        return route;
                    }

                    queue.Enqueue(next);
                }
            }

            return null;
        }

        private static string Flatten(string s) =>
            new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        /// <summary>
        /// Format volontairement trivial — une ligne par zone, « zone: voisine, voisine » — pour
        /// rester lisible et réparable à la main. Un plan corrompu ne doit jamais empêcher le mod
        /// de démarrer : en cas d'échec, on repart d'un plan vide et on réapprend.
        /// </summary>
        private static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (!File.Exists(FilePath)) return;

                foreach (string line in File.ReadAllLines(FilePath))
                {
                    int colon = line.IndexOf(':');
                    if (colon <= 0) continue;

                    string scene = line.Substring(0, colon).Trim();
                    if (scene.Length == 0) continue;

                    var exits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (string exit in line.Substring(colon + 1).Split(','))
                    {
                        string trimmed = exit.Trim();
                        if (trimmed.Length > 0) exits.Add(trimmed);
                    }
                    _links[scene] = exits;
                }

                Plugin.Log?.LogInfo($"Plan des liaisons : {_links.Count} zones connues.");
            }
            catch (Exception e)
            {
                _links.Clear();
                Plugin.Log?.LogWarning("Plan des liaisons illisible, on repart de zéro : " + e.Message);
            }
        }

        private static void Save()
        {
            if (!_dirty) return;
            _dirty = false;

            try
            {
                File.WriteAllLines(FilePath,
                    _links.Where(p => p.Value.Count > 0)
                          .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
                          .Select(p => p.Key + ": " + string.Join(", ", p.Value.OrderBy(v => v).ToArray()))
                          .ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Plan des liaisons non enregistré : " + e.Message);
            }
        }
    }
}
