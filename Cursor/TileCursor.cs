using UnityEngine;
using UnityEngine.Tilemaps;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Localization;
using SunHavenAccess.Util;
using SunHavenAccess.Dialogue;

namespace SunHavenAccess.Cursor
{
    /// <summary>
    /// Le "curseur de case" : toujours la case juste devant le joueur, dans la direction où
    /// il regarde (comme le tile viewer de stardew-access). On réutilise directement le
    /// système d'interaction du jeu (Wish.PlayerInteractions), qui trie déjà les objets
    /// interactifs par proximité dans la direction du regard : c'est exactement l'objet que
    /// le jeu met en surbrillance à l'écran, donc la description vocale correspond toujours
    /// à ce qui serait activé en appuyant sur la touche d'interaction.
    /// Pour les cultures spécifiquement, on construit notre propre description (état arrosé,
    /// stade de croissance) plutôt que d'utiliser le texte d'interaction du jeu, trop laconique
    /// ("Harvest") et pas toujours en français. Pour une case labourée SANS rien de planté, on
    /// interroge directement les données de la ferme. En tout dernier repli, une détection
    /// physique simple signale un obstacle "muet" (mur, décor non-interactif...) plutôt que
    /// d'annoncer à tort que la voie est libre.
    /// </summary>
    public static class TileCursor
    {
        private static Vector2Int _lastTile;
        private static Direction _lastDir;
        private static bool _hasLast;
        private static bool _verbose = true;

        // Ordre horaire, pour "tourner à gauche/à droite" sans se déplacer.
        private static readonly Direction[] ClockwiseOrder =
        {
            Direction.North, Direction.East, Direction.South, Direction.West
        };

        public static void ToggleVerbosity()
        {
            _verbose = !_verbose;
            TolkSpeech.Speak(_verbose
                ? "Annonce automatique des cases activée."
                : "Annonce automatique des cases désactivée. Utilisez la touche dédiée pour l'annoncer manuellement.", true);
        }

        /// <summary>Appelé chaque frame : annonce la case en face uniquement si elle a changé.</summary>
        public static void Tick()
        {
            Player player = Player.Instance;
            if (player == null) return;
            if (DialogueReader.DialogueOnGoing) return; // ne pas parasiter une conversation en cours
            if (!_verbose) return;

            Vector2Int tile = player.Position;
            Direction dir = player.facingDirection;
            if (_hasLast && tile == _lastTile && dir == _lastDir) return;
            _lastTile = tile;
            _lastDir = dir;
            _hasLast = true;

            // Annonce SYSTÉMATIQUEMENT à chaque case/direction différente, même si le contenu
            // est identique au pas précédent (ex. plusieurs cases d'herbe à la suite) : un
            // ancien filtre par contenu rendait le mod silencieux en traversant une zone
            // uniforme, ce qui ne permettait pas de suivre chaque pas comme demandé.
            string desc = BuildFrontDescription(player);
            TolkSpeech.Speak(desc, interrupt: false);
        }

        /// <summary>Touche dédiée : décrit la case en face, que la verbosité auto soit activée ou non.</summary>
        public static void AnnounceFront()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }
            TolkSpeech.Speak(BuildFrontDescription(player), interrupt: true);
        }

        public static void AnnouncePosition()
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }
            Vector2Int p = player.Position;
            TolkSpeech.Speak(
                $"Position {p.x}, {p.y}. Vous regardez vers le {Strings.DirectionName(player.facingDirection)}.",
                true);
        }

        /// <summary>Tourne le personnage de 90° sans le déplacer. direction : -1 = gauche, +1 = droite.</summary>
        public static void Turn(int direction)
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            int idx = System.Array.IndexOf(ClockwiseOrder, player.facingDirection);
            if (idx < 0) idx = 0;
            idx = ((idx + direction) % ClockwiseOrder.Length + ClockwiseOrder.Length) % ClockwiseOrder.Length;
            player.facingDirection = ClockwiseOrder[idx];
            player.UpdateFacingDirection();

            // Annonce immédiatement la nouvelle case en face, comme si le joueur venait de
            // tourner la tête — évite d'avoir à appuyer sur une deuxième touche.
            _lastTile = player.Position;
            _lastDir = player.facingDirection;
            _hasLast = true;
            string desc = BuildFrontDescription(player);
            TolkSpeech.Speak(desc, interrupt: true);
        }

        private static string BuildFrontDescription(Player player)
        {
            string facing = Strings.DirectionName(player.facingDirection);

            PlayerInteractions interactions = player.GetComponent<PlayerInteractions>();
            if (interactions != null && interactions.Interactables.Count > 0)
            {
                Interaction first = interactions.Interactables[0];
                IInteractable target = first.interactable;

                if (target is Crop crop)
                {
                    return DescribeCrop(crop);
                }

                InteractionInfo info = target?.InteractionPoint;
                if (info?.interactionText != null && info.interactionText.Count > 0)
                {
                    int idx = Mathf.Clamp(first.interactionType, 0, info.interactionText.Count - 1);
                    string text = TextUtil.Clean(info.interactionText[idx]);
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            string farmland = DescribeFarmland(player);
            if (farmland != null) return farmland;

            string obstacle = DescribeObstacle(player, facing);
            if (obstacle != null) return obstacle;

            // Repli sur le TYPE de terrain (herbe, chemin, sable, pierre...) lu directement dans
            // la tuilemap du sol, pour ne dire "rien devant vous" que quand vraiment aucune
            // donnée de case n'est trouvée nulle part (demandé : chaque case a un contenu réel,
            // même en dehors des zones cultivables).
            string ground = DescribeGroundTile(player);
            if (ground != null) return $"{ground}, côté {facing}.";

            return $"Rien devant vous, côté {facing}.";
        }

        /// <summary>
        /// Nom de la tuile de sol sous la case en face (herbe, sable, pierre, chemin...) et
        /// présence d'eau. Premier essai (22/08/2026) ne renvoyait jamais rien : la cause était
        /// `Tilemap.WorldToCell(worldPos)`, qui utilise la transformation de grille standard
        /// d'Unity — alors qu'en décompilant `Wish.GameManager` (méthodes `GetBottomTile`/
        /// `IsBottomTileType`), le jeu indexe TOUJOURS ses tuilemaps directement par coordonnée
        /// de case entière (`new Vector3Int(position.x, position.y, 0)`, la même que
        /// `Player.Position`), jamais via WorldToCell. Corrigé pour faire pareil. On utilise
        /// aussi `GameManager.Instance.TileMaps` (la liste DÉJÀ filtrée par le jeu : elle exclut
        /// `dataLayer`/`topLayer`/les tuilemaps d'ennemis) au lieu de chercher nous-mêmes tous
        /// les Tilemap de la scène.
        /// En complément, `GameManager.Instance.dataLayer` porte une couche de métadonnées
        /// invisible (`Wish.DataTile`, jamais affichée à l'écran mais toujours interrogeable) où
        /// `waterType` (None/Water/FishableWater) est fiable à 100% (une valeur d'enum du jeu,
        /// pas un nom d'objet deviné) : utilisé en priorité pour signaler l'eau, plus important
        /// à savoir pour un joueur aveugle qu'un simple nom de texture.
        /// </summary>
        private static string DescribeGroundTile(Player player)
        {
            try
            {
                Vector2Int frontTile = GetFrontTileCoord(player);
                var cell = new Vector3Int(frontTile.x, frontTile.y, 0);

                GameManager gameManager = SingletonBehaviour<GameManager>.Instance;
                if (gameManager == null) return null;

                string water = DescribeWater(gameManager, cell);

                string groundName = null;
                foreach (Tilemap tilemap in gameManager.TileMaps)
                {
                    if (tilemap == null) continue;
                    TileBase tile = tilemap.GetTile(cell);
                    if (tile == null) continue;
                    string translated = UiNameTranslator.Translate(tile.name);
                    if (!string.IsNullOrWhiteSpace(translated)) { groundName = translated; break; }
                }

                if (water != null && groundName != null) return $"{groundName}, {water}";
                return water ?? groundName;
            }
            catch
            {
                // Best-effort : en cas de souci (tuilemap inattendue...), on retombe sur le
                // message générique plutôt que de planter.
                return null;
            }
        }

        private static string DescribeWater(GameManager gameManager, Vector3Int cell)
        {
            if (gameManager.dataLayer == null) return null;
            DataTile dataTile = gameManager.dataLayer.GetTile<DataTile>(cell);
            if (dataTile == null) return null;
            return dataTile.waterType switch
            {
                WaterType.FishableWater => "eau où l'on peut pêcher",
                WaterType.Water => "eau",
                _ => null,
            };
        }

        /// <summary>Même formule que Player.Position, décalée d'une case dans la direction regardée.</summary>
        private static Vector2Int GetFrontTileCoord(Player player)
        {
            Vector2Int offset = player.facingDirection switch
            {
                Direction.North => new Vector2Int(0, 1),
                Direction.South => new Vector2Int(0, -1),
                Direction.East => new Vector2Int(1, 0),
                _ => new Vector2Int(-1, 0),
            };
            return player.Position + offset;
        }

        /// <summary>
        /// Description d'une culture : état arrosé et stade de croissance, plutôt que le verbe
        /// d'interaction du jeu ("Harvest"), trop laconique et pas toujours en français.
        /// Utilisé à la fois par le curseur de case et par le scanner (Navigation/Scanner.cs).
        /// </summary>
        public static string DescribeCrop(Crop crop)
        {
            if (crop.data == null) return "Culture.";

            if (crop.data.dead) return "Culture morte.";

            string watered = crop.data.watered ? "arrosée" : "non arrosée";
            string growth = crop.CheckGrowth
                ? "prête à récolter"
                : $"encore {Mathf.Max(crop.DaysLeft, 0)} jour{(crop.DaysLeft > 1 ? "s" : "")} avant maturité";

            return $"Culture, {watered}, {growth}.";
        }

        /// <summary>
        /// Quand rien n'est planté sur la case en face, on regarde quand même si c'est une
        /// case de terre cultivable/labourée/arrosée, pour l'annoncer comme le ferait
        /// stardew-access (au lieu de dire "rien devant vous" sur une case de plantation vide).
        /// </summary>
        private static string DescribeFarmland(Player player)
        {
            try
            {
                Vector2Int frontTile = GetFrontTileCoord(player);

                FarmingTileInfo info = SingletonBehaviour<GameSave>.Instance.GetFarmingInfo(
                    frontTile, ScenePortalManager.ActiveSceneIndex);

                return info switch
                {
                    FarmingTileInfo.Farmable => "Terre cultivable, pas encore labourée.",
                    FarmingTileInfo.Hoed => "Terre labourée, pas arrosée.",
                    FarmingTileInfo.Watered => "Terre labourée et arrosée, rien de planté.",
                    _ => null,
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Repli quand rien d'interactif ni de cultivable n'est détecté : vérifie s'il y a
        /// quand même un obstacle physique simple (mur, rocher non ciblable, décor...) sur la
        /// case en face, pour ne pas annoncer à tort "rien devant vous" alors que le passage
        /// est bloqué.
        /// </summary>
        private static string DescribeObstacle(Player player, string facing)
        {
            try
            {
                Vector3 frontPos = player.transform.position + Utilities.OffsetFromDirection(player.facingDirection);
                Collider2D[] hits = Physics2D.OverlapCircleAll(frontPos, 0.35f);
                foreach (Collider2D hit in hits)
                {
                    if (hit == null || hit.isTrigger) continue;
                    if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == player.gameObject) continue;
                    if (hit.transform.IsChildOf(player.transform)) continue;
                    return $"Quelque chose bloque le passage devant vous, côté {facing}.";
                }
            }
            catch
            {
                // Détection best-effort : en cas de souci, on se contente du message par défaut.
            }
            return null;
        }
    }
}
