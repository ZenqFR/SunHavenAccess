using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Wish;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Marque un point d'eau. Comme les favoris, il existe pour que le scanner ait un objet à
    /// trouver là où le jeu n'en a pas.
    /// </summary>
    public class WaterMarker : MonoBehaviour
    {
        public string WaterName;
    }

    /// <summary>
    /// Les points d'eau, que le jeu ne range dans aucun objet.
    ///
    /// POURQUOI ILS MANQUAIENT. Tout ce que le scanner trouvait jusqu'ici était un objet : un
    /// rocher, une porte, un habitant. L'eau, elle, n'est pas un objet — c'est une propriété de la
    /// case, `DataTile.waterType`. Elle échappait donc complètement au scanner, alors qu'on la
    /// cherche tout le temps : remplir un arrosoir, trouver où pêcher. Le seul moyen était de
    /// tomber dessus.
    ///
    /// COMMENT ON LES TROUVE. On parcourt les cases autour du joueur et l'on retient celles qui
    /// portent de l'eau. C'est un balayage de grille, donc on le fait UNIQUEMENT quand la catégorie
    /// est demandée, jamais en fond, et sur un rayon volontairement court : au-delà d'une vingtaine
    /// de cases, on ne cherche plus un point d'eau, on explore.
    ///
    /// ON REGROUPE LES CASES EN PLANS D'EAU. Une rivière, ce sont des centaines de cases voisines,
    /// et les annoncer une par une donnerait une liste illisible pour une seule et même rivière. On
    /// ne garde donc qu'un point tous les quatre cases : assez pour distinguer deux étangs, assez
    /// peu pour ne pas répéter le même bord de lac vingt fois.
    /// </summary>
    internal static class WaterFinder
    {
        /// <summary>
        /// Rayon de recherche, en cases. Court par choix : un balayage de grille coûte le carré de
        /// ce nombre, et l'eau qu'on cherche est celle où l'on peut aller à pied maintenant.
        /// </summary>
        private const int Radius = 22;

        /// <summary>Espacement minimal entre deux points retenus, pour ne pas décrire une rivière case par case.</summary>
        private const int Spacing = 4;

        private static readonly List<GameObject> _markers = new List<GameObject>();

        internal static IEnumerable<Component> MarkersHere()
        {
            Clear();

            Player player = Player.Instance;
            GameManager manager = SingletonBehaviour<GameManager>.Instance;
            if (player == null || manager?.dataLayer == null) yield break;

            Vector2Int centre = player.Position;
            var kept = new List<Vector2Int>();

            for (int dy = -Radius; dy <= Radius; dy++)
            {
                for (int dx = -Radius; dx <= Radius; dx++)
                {
                    var tile = new Vector2Int(centre.x + dx, centre.y + dy);

                    string label = WaterAt(manager, tile);
                    if (label == null) continue;

                    if (TooCloseToKept(kept, tile)) continue;
                    kept.Add(tile);

                    var go = new GameObject($"Eau : {label}");
                    go.transform.position = Util.TileGeometry.TileToWorld(tile);

                    WaterMarker marker = go.AddComponent<WaterMarker>();
                    marker.WaterName = label;

                    _markers.Add(go);
                    yield return marker;
                }
            }
        }

        private static bool TooCloseToKept(List<Vector2Int> kept, Vector2Int tile)
        {
            foreach (Vector2Int other in kept)
            {
                if (Mathf.Abs(other.x - tile.x) < Spacing && Mathf.Abs(other.y - tile.y) < Spacing) return true;
            }
            return false;
        }

        /// <summary>
        /// Le type d'eau d'une case, ou null. On distingue l'eau où l'on peut pêcher du reste :
        /// c'est la seule différence qui change ce qu'on vient y faire.
        /// </summary>
        private static string WaterAt(GameManager manager, Vector2Int tile)
        {
            try
            {
                DataTile data = manager.dataLayer.GetTile<DataTile>(new Vector3Int(tile.x, tile.y, 0));
                if (data == null) return null;

                return data.waterType switch
                {
                    WaterType.FishableWater => Localization.Language.T("eau poissonneuse", "fishable water"),
                    WaterType.Water => Localization.Language.T("eau", "water"),
                    _ => null,
                };
            }
            catch { return null; }
        }

        private static void Clear()
        {
            foreach (GameObject go in _markers)
            {
                if (go != null) Object.Destroy(go);
            }
            _markers.Clear();
        }
    }
}
