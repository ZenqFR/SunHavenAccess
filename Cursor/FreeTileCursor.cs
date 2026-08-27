using UnityEngine;
using Wish;
using SunHavenAccess.Localization;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Cursor
{
    /// <summary>
    /// Curseur de case LIBRE : un point de lecture déplaçable aux flèches n'importe où sur la
    /// carte, indépendamment du personnage. Équivalent du « Tile Viewer » de stardew-access, et
    /// jusqu'ici le plus gros manque du mod — on ne savait lire que « la case devant moi », ce qui
    /// rend l'exploration d'une carte inconnue quasi impossible : aucun moyen de savoir ce qu'il y
    /// a trois cases plus loin sans s'y rendre physiquement.
    ///
    /// Trois usages : explorer sans bouger, lancer un cheminement vers la case visée, et pointer
    /// la souris dessus pour y agir à distance (beaucoup d'actions de Sun Haven — arroser,
    /// labourer, récolter, miner — passent par un clic souris).
    ///
    /// Les flèches ne sont liées à AUCUNE action du jeu (déplacement en ZQSD/WASD), elles sont
    /// donc libres en jeu. Mais elles servent à la navigation de menus : ce curseur ne les capte
    /// QUE hors menu, sinon il volerait les flèches à `ZoneNavigator` — exactement le piège qui a
    /// déjà cassé la navigation deux fois.
    /// </summary>
    public static class FreeTileCursor
    {
        private static bool _active;
        private static Vector2Int _tile;

        public static bool Active => _active;

        /// <summary>Case actuellement visée. N'a de sens que si le curseur est actif.</summary>
        public static Vector2Int Tile => _tile;

        public static void Toggle()
        {
            if (_active)
            {
                _active = false;
                TolkSpeech.Speak("Curseur libre désactivé.", true);
                return;
            }

            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            // On démarre TOUJOURS sur la case du joueur : ça donne un repère connu, et ça sert
            // d'autovérification de la conversion — la description doit correspondre à l'endroit
            // où l'on se tient réellement.
            _active = true;
            _tile = player.Position;
            TolkSpeech.Speak(Localization.Language.T($"Curseur libre activé. {Describe(includeBearing: false)}", $"Free cursor on. {Describe(includeBearing: false)}"), true);
        }

        public static void Deactivate() => _active = false;

        /// <summary>Recentre sur le joueur sans quitter le mode curseur.</summary>
        public static void Recenter()
        {
            Player player = Player.Instance;
            if (!_active || player == null) return;
            _tile = player.Position;
            TolkSpeech.Speak(Localization.Language.T($"Recentré. {Describe(includeBearing: false)}", $"Recentred. {Describe(includeBearing: false)}"), true);
        }

        public static void Move(int dx, int dy)
        {
            if (!_active) return;

            var moved = new Vector2Int(_tile.x + dx, _tile.y + dy);
            if (!TileExists(moved))
            {
                // Bord de la carte chargée : même retour sonore que les bords de zone en menu.
                UiSound.EdgeBump();
                return;
            }

            _tile = moved;
            TolkSpeech.Speak(Describe(includeBearing: true), true);
        }

        public static void AnnounceCurrent()
        {
            if (!_active) return;
            TolkSpeech.Speak(Describe(includeBearing: true), true);
        }

        /// <summary>Cheminement automatique vers la case visée.</summary>
        public static void TravelToCursor()
        {
            if (!_active) return;
            // PathingController annonce lui-même tous les cas : succès, trop loin, chemin bloqué.
            PathingController.TravelTo(TileGeometry.TileToWorld(_tile), Localization.Language.T("la case visée", "the targeted tile"));
        }

        /// <summary>
        /// Pointe la souris Windows sur la case visée, pour qu'un clic simulé y agisse plutôt que
        /// sur la case devant le personnage.
        /// </summary>
        public static bool PointMouseAtCursor()
        {
            if (!_active) return false;
            return MouseCursor.PointAt(TileGeometry.TileToWorld(_tile));
        }

        // ------------------------------------------------------------ Description

        private static string Describe(bool includeBearing)
        {
            string content = DescribeTileContent(_tile) ?? "Rien";

            if (!includeBearing) return $"{content}.";

            Player player = Player.Instance;
            if (player == null) return $"{content}.";

            Vector2Int delta = _tile - player.Position;
            if (delta == Vector2Int.zero) return $"{content}, sous vos pieds.";

            // En coordonnées de CASES, les deux axes sont déjà homogènes (1 case = 1 unité) : la
            // distance se calcule directement, sans la correction isométrique qu'exigerait
            // l'espace monde.
            int distance = Mathf.RoundToInt(new Vector2(delta.x, delta.y).magnitude);
            string bearing = Strings.BearingName(new Vector2(delta.x, delta.y));
            return $"{content}, {bearing}, {distance} case{(distance > 1 ? "s" : "")}.";
        }

        /// <summary>
        /// Contenu d'une case arbitraire, en réutilisant les descriptions de TileCursor.
        ///
        /// Différence essentielle avec « la case devant vous » : on ne peut PAS réutiliser
        /// `PlayerInteractions.Interactables`, la liste du jeu triée par proximité ET par
        /// direction du regard — elle ne couvre que l'entourage immédiat. Pour une case éloignée
        /// il faut sonder physiquement la position, comme le fait déjà `Scanner`.
        /// </summary>
        private static string DescribeTileContent(Vector2Int tile)
        {
            string interactable = DescribeInteractableAt(tile);
            if (interactable != null) return interactable;

            string farmland = TileCursor.DescribeFarmland(tile);
            if (farmland != null) return farmland;

            return TileCursor.DescribeGroundTile(tile);
        }

        private static string DescribeInteractableAt(Vector2Int tile)
        {
            try
            {
                Vector3 world = TileGeometry.TileToWorld(tile);

                // Boîte à l'échelle d'UNE case : 1 unité de large, mais 1 case de haut EN MONDE
                // (donc étirée de YScale) — sinon on ramasserait le contenu des cases voisines
                // au-dessus et en dessous, le monde étant isométrique.
                Collider2D[] hits = Physics2D.OverlapBoxAll(
                    world, new Vector2(0.9f, 0.9f * TileGeometry.YScale), 0f);

                foreach (Collider2D hit in hits)
                {
                    if (hit == null) continue;

                    Crop crop = hit.GetComponentInParent<Crop>();
                    if (crop != null) return TileCursor.DescribeCrop(crop);

                    var interactable = hit.GetComponentInParent<IInteractable>();
                    if (interactable == null) continue;

                    InteractionInfo info = interactable.InteractionPoint;
                    if (info?.interactionText != null && info.interactionText.Count > 0)
                    {
                        string text = TextUtil.Clean(info.interactionText[0]);
                        if (!string.IsNullOrWhiteSpace(text)) return text;
                    }
                }
            }
            catch
            {
                // Sondage best-effort : on retombe sur la description du terrain.
            }
            return null;
        }

        /// <summary>
        /// La case appartient-elle encore à la carte chargée ? Garde-fou pour ne pas laisser le
        /// curseur filer indéfiniment dans le vide.
        /// </summary>
        private static bool TileExists(Vector2Int tile)
        {
            try
            {
                GameManager gameManager = SingletonBehaviour<GameManager>.Instance;
                if (gameManager == null) return false;

                var cell = new Vector3Int(tile.x, tile.y, 0);
                foreach (UnityEngine.Tilemaps.Tilemap tilemap in gameManager.TileMaps)
                {
                    if (tilemap != null && tilemap.GetTile(cell) != null) return true;
                }
                return false;
            }
            catch
            {
                // En cas de doute on laisse passer : mieux vaut un curseur trop permissif qu'un
                // curseur bloqué sur place.
                return true;
            }
        }
    }
}
