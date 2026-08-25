using UnityEngine;
using Wish;

namespace SunHavenAccess.Util
{
    /// <summary>
    /// LA conversion entre coordonnées de case et position dans le monde. Point unique et
    /// autoritatif : avant cette classe, la conversion était recopiée à cinq endroits avec DEUX
    /// conventions contradictoires, et un commentaire du mod affirmait à tort que
    /// `Player.Position` était une simple troncature des coordonnées monde.
    ///
    /// La vérité vient de la définition du jeu lui-même (`Wish.Player.Position`, décompilé) :
    ///
    ///     Position => new Vector2Int((int)transform.position.x,
    ///                                (int)(transform.position.y / 1.4142135f + 0.375f))
    ///
    /// Le monde est donc isométrique : une case fait 1 unité en X mais 1,4142135 en Y, avec un
    /// décalage de 0,375. C'est cette convention — et non la troncature simple — qui est juste,
    /// ce qui confirme au passage celle qu'utilisaient déjà `PathingController`, `Scanner`,
    /// `NPCFinder` et `Strings.BearingName`.
    ///
    /// Attention : les TUILEMAPS du jeu, elles, s'indexent bien par ces coordonnées de case
    /// entières (`tilemap.GetTile(new Vector3Int(tile.x, tile.y, 0))`, cf. `GameManager.
    /// GetBottomTile`) — jamais via `Tilemap.WorldToCell`. Les deux faits sont compatibles :
    /// l'indexation se fait en espace de cases, la position physique en espace monde.
    /// </summary>
    public static class TileGeometry
    {
        /// <summary>Hauteur d'une case en unités monde, telle que définie par Wish.Player.Position.</summary>
        public const float YScale = 1.4142135f;

        /// <summary>Décalage vertical appliqué par le jeu avant troncature.</summary>
        private const float YOffset = 0.375f;

        /// <summary>
        /// Case contenant une position monde. Reproduit EXACTEMENT la formule de
        /// `Wish.Player.Position`, pour que la case du joueur calculée ici soit toujours celle que
        /// le jeu lui-même considère.
        /// </summary>
        public static Vector2Int WorldToTile(Vector3 world) =>
            new Vector2Int((int)world.x, (int)(world.y / YScale + YOffset));

        /// <summary>
        /// CENTRE d'une case, en position monde. Inverse de WorldToTile : la troncature de
        /// `Position` accepte tout y tel que (y / YScale + YOffset) tombe dans [tile.y, tile.y+1),
        /// on vise donc le milieu de cet intervalle pour être le plus loin possible des deux bords
        /// (une erreur d'arrondi ne ferait alors pas basculer sur la case voisine).
        /// </summary>
        public static Vector3 TileToWorld(Vector2Int tile) =>
            new Vector3(tile.x + 0.5f, (tile.y + 0.5f - YOffset) * YScale, 0f);

        /// <summary>
        /// Distance en cases entre deux positions monde, corrigée de l'étirement vertical — sans
        /// quoi un déplacement vertical paraîtrait 1,4 fois plus long qu'un horizontal.
        /// </summary>
        public static float TileDistance(Vector3 a, Vector3 b)
        {
            Vector3 delta = a - b;
            return new Vector2(delta.x, delta.y / YScale).magnitude;
        }
    }
}
