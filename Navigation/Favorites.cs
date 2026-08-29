using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using Wish;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Marque un point favori dans le monde. Existe pour que le scanner puisse le trouver comme
    /// n'importe quoi d'autre : il cherche des objets, on lui en donne un.
    /// </summary>
    public class FavoriteMarker : MonoBehaviour
    {
        public string FavoriteName;
    }

    /// <summary>
    /// Les points favoris : nommer un endroit, le retrouver, y retourner.
    ///
    /// POURQUOI. Le scanner trouve ce que le JEU connaît — un rocher, une porte, un habitant. Il ne
    /// connaît pas « l'endroit où je plante mes navets », « le coin où je pêche », « là où j'ai
    /// laissé mes coffres ». Ces repères-là n'existent nulle part dans le jeu : ils sont dans la
    /// tête de qui joue, et pour qui voit ils tiennent à un coup d'œil sur le paysage. Sans la vue,
    /// il n'y a rien pour les tenir.
    ///
    /// COMMENT. On pose un point là où l'on se tient, on lui donne le nom qu'on veut, et le
    /// scanner le trouve désormais dans sa propre catégorie, avec sa distance — donc le trajet
    /// automatique y mène comme partout ailleurs. Renommer et supprimer se font au même endroit.
    ///
    /// CE QUI LES REND UTILISABLES PAR LE SCANNER. Le scanner cherche des objets ; un favori n'est
    /// qu'un nom et deux nombres. On crée donc un objet invisible à sa position, à la demande, qui
    /// n'existe que le temps qu'on s'en serve. Rien n'est affiché, rien ne gêne le jeu, et le
    /// scanner n'a pas à connaître un cas particulier de plus.
    ///
    /// OÙ ILS VIVENT. Dans un fichier texte à côté de la configuration, une ligne par point, format
    /// volontairement trivial pour rester lisible et réparable à la main. Ils survivent donc aux
    /// parties, comme le plan des liaisons.
    ///
    /// COÛT EN FOND : nul. Rien n'est créé ni relu tant qu'on ne demande pas les favoris.
    /// </summary>
    internal static class Favorites
    {
        internal sealed class Point
        {
            internal string Name;
            internal string Scene;
            internal float X;
            internal float Y;
        }

        private static readonly List<Point> _points = new List<Point>();
        private static bool _loaded;

        private static string FilePath =>
            Path.Combine(BepInEx.Paths.ConfigPath, "SunHavenAccess-favoris.txt");

        internal static List<Point> All()
        {
            Load();
            return _points;
        }

        /// <summary>Les favoris de la zone où l'on se trouve, du plus proche au plus loin.</summary>
        internal static List<Point> Here()
        {
            Load();
            string here = WorldLinks.CurrentScene;
            Vector3 from = Player.Instance != null ? Player.Instance.transform.position : Vector3.zero;

            return _points
                .Where(p => string.Equals(p.Scene, here, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => Vector2.Distance(new Vector2(p.X, p.Y), new Vector2(from.x, from.y)))
                .ToList();
        }

        /// <summary>
        /// Ajoute un point à l'endroit exact où se tient le personnage. C'est le seul endroit dont
        /// on soit certain qu'il est atteignable : on y est.
        /// </summary>
        internal static bool AddHere(string name)
        {
            Player player = Player.Instance;
            if (player == null || string.IsNullOrWhiteSpace(name)) return false;

            Load();
            Vector3 p = player.transform.position;
            _points.Add(new Point
            {
                Name = name.Trim(),
                Scene = WorldLinks.CurrentScene ?? string.Empty,
                X = p.x,
                Y = p.y,
            });

            Save();
            return true;
        }

        internal static void Rename(Point point, string name)
        {
            if (point == null || string.IsNullOrWhiteSpace(name)) return;
            point.Name = name.Trim();
            Save();
        }

        internal static void Remove(Point point)
        {
            if (point == null) return;
            Load();
            _points.Remove(point);
            Save();
            ClearMarkers();
        }

        // ---------------------------------------------------------------- Repères pour le scanner

        private static readonly List<GameObject> _markers = new List<GameObject>();

        /// <summary>
        /// Les repères des favoris de la zone courante, créés à la demande.
        ///
        /// On les reconstruit à chaque appel plutôt que de les suivre : entre deux relevés on a pu
        /// changer de zone, en ajouter, en supprimer. Reconstruire quelques objets vides coûte
        /// moins cher que de tenir à jour une correspondance qui aurait sa propre façon de se
        /// tromper.
        /// </summary>
        internal static IEnumerable<Component> MarkersHere()
        {
            ClearMarkers();

            foreach (Point point in Here())
            {
                var go = new GameObject($"Favori : {point.Name}");
                go.transform.position = new Vector3(point.X, point.Y, 0f);

                FavoriteMarker marker = go.AddComponent<FavoriteMarker>();
                marker.FavoriteName = point.Name;

                _markers.Add(go);
                yield return marker;
            }
        }

        private static void ClearMarkers()
        {
            foreach (GameObject go in _markers)
            {
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _markers.Clear();
        }

        // ---------------------------------------------------------------------------- Persistance

        /// <summary>
        /// Une ligne par point : zone, x, y, nom. Le nom vient en dernier et peut donc contenir des
        /// barres verticales sans casser la lecture. Les nombres sont écrits en culture invariante,
        /// sans quoi un fichier écrit sur un système français — virgule décimale — serait illisible
        /// ailleurs, et inversement.
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
                    string[] parts = line.Split(new[] { '|' }, 4);
                    if (parts.Length < 4) continue;

                    if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                    if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;

                    string name = parts[3].Trim();
                    if (name.Length == 0) continue;

                    _points.Add(new Point { Scene = parts[0].Trim(), X = x, Y = y, Name = name });
                }

                Plugin.Log?.LogInfo($"Points favoris : {_points.Count} enregistré(s).");
            }
            catch (Exception e)
            {
                // Un fichier abîmé ne doit jamais empêcher de jouer : on repart de rien plutôt que
                // de laisser le mod en échec. Ce qui a été perdu se repose en quelques secondes.
                _points.Clear();
                Plugin.Log?.LogWarning("Points favoris illisibles, on repart de zéro : " + e.Message);
            }
        }

        private static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, _points
                    .Select(p => string.Join("|", new[]
                    {
                        p.Scene,
                        p.X.ToString("R", CultureInfo.InvariantCulture),
                        p.Y.ToString("R", CultureInfo.InvariantCulture),
                        p.Name,
                    }))
                    .ToArray());
            }
            catch (Exception e)
            {
                Plugin.Log?.LogWarning("Points favoris non enregistrés : " + e.Message);
            }
        }
    }
}
