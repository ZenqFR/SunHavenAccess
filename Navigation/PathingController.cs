using System;
using System.Collections.Generic;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Cheminement automatique façon stardew-access (Ctrl+Origine sur le scanner).
    ///
    /// D'ABORD essayé en réutilisant les classes globales `Pathfinding`/`Grid`/`Node`/`Heap` du
    /// DLL du jeu : décompilation complète du projet (`ilspycmd -p`) + recherche de tous les
    /// appelants de `RequestPath(` a montré que c'est le tutoriel A* de Sebastian Lague resté tel
    /// quel dans le build (jusqu'à sa classe `Unit` de démo), JAMAIS utilisé par Sun Haven — donc
    /// jamais testé non plus. En le faisant tourner nous-mêmes (grille temporaire créée à la
    /// volée), il plantait (IndexOutOfRangeException dans son `Heap<T>` à capacité fixe, et
    /// `Grid.NodeFromWorldPoint` ignore carrément `transform.position` alors que `CreateGrid` en
    /// dépend — une vraie incohérence interne du code jamais remarquée faute d'usage réel).
    ///
    /// Solution retenue : un A* minimal écrit ici, indépendant de ce code mort, sur une grille de
    /// cases calculée à la demande autour du segment joueur→cible. La marche/collision réelle
    /// (Physics2D.OverlapBox + masque déduit de la VRAIE matrice de collision Unity du calque du
    /// joueur, pas un nom de calque deviné) reste la même idée que le code du jeu, juste
    /// réimplémentée proprement avec un tas dynamique (pas de capacité fixe, donc pas de
    /// débordement possible).
    /// </summary>
    public static class PathingController
    {
        private const float MoveSpeed = 4.5f;
        private const int MarginTiles = 8;
        private const int MaxGridTiles = 100; // par axe ; au-delà, on refuse plutôt que de risquer une grille énorme
        private const float YScale = 1.4142135f; // même facteur isométrique que tout le reste du mod

        public static bool IsPathing => Player.Instance != null && Player.Instance.Pathing;

        public static void TravelTo(Vector3 worldPosition, string destinationLabel)
        {
            Player player = Player.Instance;
            if (player == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            PathResult result;
            try
            {
                result = FindPath(player.transform.position, worldPosition, player.gameObject.layer);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("PathingController.FindPath a levé une exception : " + e);
                TolkSpeech.Speak("Erreur pendant le calcul du chemin.", true);
                return;
            }

            if (result.TooFar)
            {
                TolkSpeech.Speak($"{destinationLabel} est trop loin pour un cheminement automatique.", true);
                return;
            }
            if (result.Waypoints.Length == 0)
            {
                // Même la première case autour du joueur est bloquée : aucun pas possible.
                TolkSpeech.Speak($"Impossible de bouger vers {destinationLabel}, le passage est bloqué.", true);
                return;
            }

            var path = new List<PathDescription>(result.Waypoints.Length);
            foreach (Vector2 waypoint in result.Waypoints)
            {
                path.Add(new PathDescription { location = waypoint, teleport = false });
            }

            if (result.ReachedTarget)
            {
                TolkSpeech.Speak($"Cheminement vers {destinationLabel}.", true);
                player.SetFullPath(path, MoveSpeed, () =>
                {
                    TolkSpeech.Speak($"Arrivé près de {destinationLabel}.", false);
                });
            }
            else
            {
                // Demandé explicitement : si la destination exacte est inatteignable (obstacle
                // sur la suite du chemin), on avance quand même jusqu'au point le plus proche
                // réellement atteignable, plutôt que de ne rien faire du tout.
                TolkSpeech.Speak($"Chemin bloqué avant {destinationLabel} : approche au maximum.", true);
                player.SetFullPath(path, MoveSpeed, () =>
                {
                    TolkSpeech.Speak("Arrêté, obstacle sur la suite du chemin.", false);
                });
            }
        }

        /// <summary>
        /// Touche Échap : annule un cheminement en cours, sans effet sinon. StopPath() (pas
        /// CancelPath(), qui n'importe pas `Pathing` à false en décompilation) pour être sûr que
        /// les flèches directionnelles redeviennent utilisables immédiatement après.
        /// </summary>
        public static void Cancel()
        {
            Player player = Player.Instance;
            if (player == null || !player.Pathing) return;
            player.StopPath();
            TolkSpeech.Speak("Cheminement annulé.", true);
        }

        // ------------------------------------------------------------------------------------
        // A* interne — voir commentaire de classe pour pourquoi ce n'est pas le pathfinder du jeu.
        // ------------------------------------------------------------------------------------

        private readonly struct TileCoord : IEquatable<TileCoord>
        {
            public readonly int X;
            public readonly int Y;
            public TileCoord(int x, int y) { X = x; Y = y; }
            public bool Equals(TileCoord other) => X == other.X && Y == other.Y;
            public override bool Equals(object obj) => obj is TileCoord other && Equals(other);
            public override int GetHashCode() => (X * 397) ^ Y;
        }

        private sealed class PathNode
        {
            public TileCoord Coord;
            public float GCost;
            public float HCost;
            public float FCost => GCost + HCost;
            public PathNode Parent;
        }

        private readonly struct PathResult
        {
            public readonly Vector2[] Waypoints;
            public readonly bool ReachedTarget;
            public readonly bool TooFar;

            private PathResult(Vector2[] waypoints, bool reachedTarget, bool tooFar)
            {
                Waypoints = waypoints;
                ReachedTarget = reachedTarget;
                TooFar = tooFar;
            }

            public static PathResult Full(Vector2[] waypoints) => new PathResult(waypoints, true, false);
            public static PathResult Partial(Vector2[] waypoints) => new PathResult(waypoints, false, false);
            public static readonly PathResult TooFarResult = new PathResult(Array.Empty<Vector2>(), false, true);
        }

        private static TileCoord WorldToTile(Vector3 world) =>
            new TileCoord(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y / YScale));

        private static Vector2 TileToWorld(TileCoord tile) =>
            new Vector2(tile.X, tile.Y * YScale);

        private static PathResult FindPath(Vector3 fromWorld, Vector3 toWorld, int playerLayer)
        {
            TileCoord start = WorldToTile(fromWorld);
            TileCoord end = WorldToTile(toWorld);

            int minX = Mathf.Min(start.X, end.X) - MarginTiles;
            int maxX = Mathf.Max(start.X, end.X) + MarginTiles;
            int minY = Mathf.Min(start.Y, end.Y) - MarginTiles;
            int maxY = Mathf.Max(start.Y, end.Y) + MarginTiles;

            if (maxX - minX > MaxGridTiles || maxY - minY > MaxGridTiles) return PathResult.TooFarResult;

            int obstacleMask = ComputeObstacleMask(playerLayer);
            var walkableCache = new Dictionary<TileCoord, bool>();

            // Beaucoup de décorations (herbes, petits objets, zones d'interaction...) ont un
            // collider en mode Trigger — physiquement traversable, jamais un vrai obstacle — mais
            // Physics2D.OverlapBox les détecte quand même par défaut (Physics2D.queriesHitTriggers
            // est vrai globalement). Sans ce garde-fou, à peu près toutes les cases se lisaient
            // "bloquées" dès qu'une décoration traînait dessus, d'où "impossible de trouver un
            // chemin" quasi systématique. On désactive la détection des triggers juste le temps du
            // calcul, puis on restaure l'état d'origine.
            bool previousQueriesHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            try
            {
                bool IsWalkable(TileCoord t)
                {
                    if (t.X < minX || t.X > maxX || t.Y < minY || t.Y > maxY) return false;
                    if (walkableCache.TryGetValue(t, out bool cached)) return cached;
                    Vector2 world = TileToWorld(t);
                    bool walkable = Physics2D.OverlapBox(world, new Vector2(0.8f, 0.8f * YScale), 0f, obstacleMask) == null;
                    walkableCache[t] = walkable;
                    return walkable;
                }

                // Le joueur et la cible ont eux-mêmes un collider (le leur, ou celui de l'objet
                // visé) : sans ça, la case de départ ou d'arrivée peut se lire "bloquée" par ce
                // collider-là et rendre tout chemin impossible dès la première case.
                walkableCache[start] = true;
                walkableCache[end] = true;

                int maxIterations = (maxX - minX + 1) * (maxY - minY + 1) + 16;
                return RunAStar(start, end, maxIterations, IsWalkable);
            }
            finally
            {
                Physics2D.queriesHitTriggers = previousQueriesHitTriggers;
            }
        }

        private static PathResult RunAStar(TileCoord start, TileCoord end, int maxIterations, System.Func<TileCoord, bool> isWalkable)
        {
            var openSet = new List<PathNode>();
            var openLookup = new Dictionary<TileCoord, PathNode>();
            var closedSet = new HashSet<TileCoord>();

            var startNode = new PathNode { Coord = start, GCost = 0f, HCost = Heuristic(start, end) };
            openSet.Add(startNode);
            openLookup[start] = startNode;

            // Demandé explicitement : si la destination exacte est inatteignable, avancer quand
            // même jusqu'au point le plus proche réellement atteignable plutôt que de renoncer.
            // On garde donc trace, à chaque case découverte, de celle la plus proche du but
            // (HCost le plus faible) — au pire, ce sera la case de départ elle-même (aucun pas
            // possible du tout).
            PathNode bestReachable = startNode;

            PathNode endNode = null;
            int iterations = 0;

            while (openSet.Count > 0)
            {
                if (++iterations > maxIterations) break; // garde-fou anti-boucle infinie

                int bestIndex = 0;
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < openSet[bestIndex].FCost ||
                        (openSet[i].FCost == openSet[bestIndex].FCost && openSet[i].HCost < openSet[bestIndex].HCost))
                    {
                        bestIndex = i;
                    }
                }

                PathNode current = openSet[bestIndex];
                openSet.RemoveAt(bestIndex);
                openLookup.Remove(current.Coord);
                closedSet.Add(current.Coord);

                if (current.Coord.Equals(end))
                {
                    endNode = current;
                    break;
                }

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var neighbourCoord = new TileCoord(current.Coord.X + dx, current.Coord.Y + dy);
                        if (closedSet.Contains(neighbourCoord)) continue;
                        if (!isWalkable(neighbourCoord)) continue;

                        float stepCost = (dx != 0 && dy != 0) ? 1.4142135f : 1f;
                        float tentativeG = current.GCost + stepCost;

                        if (!openLookup.TryGetValue(neighbourCoord, out PathNode neighbourNode))
                        {
                            neighbourNode = new PathNode
                            {
                                Coord = neighbourCoord,
                                GCost = tentativeG,
                                HCost = Heuristic(neighbourCoord, end),
                                Parent = current
                            };
                            openSet.Add(neighbourNode);
                            openLookup[neighbourCoord] = neighbourNode;
                            if (neighbourNode.HCost < bestReachable.HCost) bestReachable = neighbourNode;
                        }
                        else if (tentativeG < neighbourNode.GCost)
                        {
                            neighbourNode.GCost = tentativeG;
                            neighbourNode.Parent = current;
                        }
                    }
                }
            }

            if (endNode != null) return PathResult.Full(BuildWaypoints(endNode));
            if (bestReachable == startNode) return PathResult.Partial(Array.Empty<Vector2>());
            return PathResult.Partial(BuildWaypoints(bestReachable));
        }

        private static Vector2[] BuildWaypoints(PathNode target)
        {
            var waypoints = new List<Vector2>();
            for (PathNode node = target; node != null; node = node.Parent)
            {
                waypoints.Add(TileToWorld(node.Coord));
            }
            waypoints.Reverse();
            return waypoints.ToArray();
        }

        /// <summary>Distance "octile" (diagonale à 1.4142135, droite à 1), cohérente avec le coût des pas.</summary>
        private static float Heuristic(TileCoord a, TileCoord b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            return Mathf.Max(dx, dy) + 0.4142135f * Mathf.Min(dx, dy);
        }

        /// <summary>
        /// Déduit quels calques peuvent physiquement bloquer le joueur depuis la vraie matrice de
        /// collision 2D d'Unity, plutôt que de deviner un nom de calque ("Obstacles"...).
        /// </summary>
        private static int ComputeObstacleMask(int selfLayer)
        {
            int mask = 0;
            var namesForLog = new List<string>();
            for (int layer = 0; layer < 32; layer++)
            {
                if (layer == selfLayer) continue;
                if (!Physics2D.GetIgnoreLayerCollision(selfLayer, layer))
                {
                    mask |= 1 << layer;
                    string layerName = LayerMask.LayerToName(layer);
                    if (!string.IsNullOrEmpty(layerName)) namesForLog.Add(layerName);
                }
            }
            // Journalisé (pas parlé) pour pouvoir diagnostiquer sans deviner si un futur test
            // échoue encore : si cette liste contient des calques qui ne devraient PHYSIQUEMENT
            // jamais bloquer le joueur (PNJ, eau décorative, UI...), c'est le signe que le masque
            // est trop large et qu'il faut l'affiner encore.
            Plugin.Log.LogInfo(
                $"PathingController : calque joueur = {LayerMask.LayerToName(selfLayer)} ({selfLayer}), " +
                $"calques considérés comme obstacles = [{string.Join(", ", namesForLog)}]");
            return mask;
        }
    }
}
