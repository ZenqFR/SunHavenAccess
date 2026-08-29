using System.Linq;
using System.Collections.Generic;
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
        private static float _lastAnnounceTime = -1f;

        // En restant immobile, la position physique du joueur (Rigidbody2D) n'est jamais
        // parfaitement fixe au pixel près — de minuscules oscillations suffisent à faire
        // basculer Player.Position (qui TRONQUE en entier, donc très sensible aux frontières de
        // case) entre deux valeurs, redéclenchant l'annonce en boucle ("spam") sans que le
        // joueur ait réellement bougé. Un léger anti-rebond temporel suffit à l'absorber sans
        // affecter les vrais pas (qui prennent bien plus de temps que ce délai).
        private const float MinSecondsBetweenAutoAnnounces = 0.35f;

        // Ordre horaire, pour "tourner à gauche/à droite" sans se déplacer.
        private static readonly Direction[] ClockwiseOrder =
        {
            Direction.North, Direction.East, Direction.South, Direction.West
        };

        public static void ToggleVerbosity()
        {
            _verbose = !_verbose;
            TolkSpeech.Speak(_verbose
                ? Localization.Language.T("Annonce automatique des cases activée.",
                                          "Automatic tile announcements on.")
                : Localization.Language.T(
                    "Annonce automatique des cases désactivée. Utilisez la touche dédiée pour l'annoncer manuellement.",
                    "Automatic tile announcements off. Use the dedicated key to ask for one."), true);
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

            // Un menu vocal a la parole : on se tait, mais on RETIENT la position. Sans cela, la
            // case courante serait annoncée d'un coup à la fermeture du menu, comme si l'on venait
            // d'y arriver. Signalé en jeu : la liste des relations était suivie, à chaque flèche,
            // d'une description du terrain mise en file derrière l'entrée qu'on voulait entendre.
            if (Menus.VoiceMenus.AnyOpen)
            {
                _lastTile = tile;
                _lastDir = dir;
                _hasLast = true;
                return;
            }

            if (_hasLast && tile == _lastTile && dir == _lastDir) return;

            // Anti-rebond : une case/direction "différente" détectée moins de
            // MinSecondsBetweenAutoAnnounces après la dernière annonce est presque toujours du
            // tremblement physique (immobile près d'une frontière de case), pas un vrai pas —
            // on l'ignore sans pour autant la retenir comme "dernière position connue", pour ne
            // pas rater une vraie annonce si le tremblement se stabilise entre-temps.
            if (_hasLast && Time.time - _lastAnnounceTime < MinSecondsBetweenAutoAnnounces) return;

            _lastTile = tile;
            _lastDir = dir;
            _hasLast = true;
            _lastAnnounceTime = Time.time;

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
            string direction = Strings.DirectionName(player.facingDirection);

            // La case sous les pieds fait partie de « où suis-je » autant que les coordonnées :
            // deux nombres situent sur une carte qu'on ne voit pas, le sol situe dans le monde.
            string underfoot = DescribeGroundTile(p);
            string ground = string.IsNullOrWhiteSpace(underfoot)
                ? string.Empty
                : Localization.Language.T($" Sol : {underfoot}.", $" Ground: {underfoot}.");

            TolkSpeech.Speak(Localization.Language.T(
                $"Position {p.x}, {p.y}. Vous regardez vers le {direction}.{ground}",
                $"Position {p.x}, {p.y}. You are facing {direction}.{ground}"),
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
            _lastAnnounceTime = Time.time;
            string desc = BuildFrontDescription(player);
            TolkSpeech.Speak(desc, interrupt: true);
        }

        /// <summary>
        /// Tout ce qu'il y a à dire de la case en face, CUMULÉ.
        ///
        /// La version précédente choisissait une seule information et abandonnait les autres :
        /// devant un rocher, on n'apprenait jamais sur quoi il reposait ; devant une culture, ni
        /// le sol ni l'état de la terre. Signalé en jeu — « faut aucun loupé, faut que ça me dise
        /// tout ». Une case porte plusieurs faits à la fois, et les faire concourir pour un seul
        /// créneau de parole en perdait à chaque pas.
        ///
        /// L'ordre suit ce qu'on veut savoir en premier : ce qui OCCUPE la case, puis l'état de la
        /// terre, puis la nature du sol, puis la direction. Le sol est passé sous silence quand
        /// l'état de la terre le dit déjà — « terre labourée, terre » ne serait qu'un bégaiement.
        /// </summary>
        private static string BuildFrontDescription(Player player)
        {
            string facing = Strings.DirectionName(player.facingDirection);
            Vector2Int front = GetFrontTileCoord(player);
            var parts = new System.Collections.Generic.List<string>();

            string occupant = DescribeOccupant(player, facing);
            if (!string.IsNullOrWhiteSpace(occupant)) parts.Add(occupant);

            string farmland = DescribeFarmland(front);
            if (!string.IsNullOrWhiteSpace(farmland)) parts.Add(farmland);

            // La terre labourée dit déjà de quoi le sol est fait.
            if (string.IsNullOrWhiteSpace(farmland))
            {
                string ground = DescribeGroundTile(front);
                if (!string.IsNullOrWhiteSpace(ground)) parts.Add(ground);
            }

            // LA DIRECTION, ICI, EST DU BRUIT — sauf si on la demande.
            //
            // Elle était rappelée à CHAQUE pas, alors qu'on vient soi-même de choisir où regarder.
            // Sur une annonce répétée des centaines de fois par partie, c'est le mot de trop qui
            // fatigue. Signalé en jeu — « ça pollue le son ». Le réglage des orientations décide,
            // et la touche de position la redonne quand on l'a perdue.
            string side = Strings.WantBearing(forTargeting: false)
                ? Localization.Language.T($", côté {facing}", $", {facing} side")
                : string.Empty;

            if (parts.Count == 0)
            {
                // Aucune donnée sur la case visée : on dit au moins sur quoi l'on se tient, seul
                // repère qui reste quand la case suivante n'en donne aucun.
                string underfoot = DescribeGroundTile(player.Position);
                return string.IsNullOrWhiteSpace(underfoot)
                    ? Localization.Language.T($"Rien devant vous{side}.",
                                              $"Nothing in front of you{side}.")
                    : Localization.Language.T(
                        $"Rien devant vous{side}. Sol sous vos pieds : {underfoot}.",
                        $"Nothing in front of you{side}. Ground underfoot: {underfoot}.");
            }

            return string.Join(", ", parts).TrimEnd('.', ' ') + side + ".";
        }

        /// <summary>
        /// Ce qui occupe la case : un objet avec lequel on peut interagir, une culture, ou un
        /// obstacle muet. Null si la case est libre.
        /// </summary>
        private static string DescribeOccupant(Player player, string facing)
        {
            PlayerInteractions interactions = player.GetComponent<PlayerInteractions>();
            if (interactions != null && interactions.Interactables.Count > 0)
            {
                Interaction first = interactions.Interactables[0];
                IInteractable target = first.interactable;

                if (target is Crop crop) return DescribeCrop(crop);

                // Ce QUE c'est passe avant ce qu'on peut en faire.
                //
                // Le texte d'interaction du jeu ne nomme que le geste — « Miner », « Ouvrir ». Sans
                // la vue, cela ne dit pas si l'on est devant de la pierre, du cuivre ou du fer, ni
                // devant quel meuble. Le descripteur du scanner, lui, sait nommer la chose ; on lui
                // demande d'abord, et le verbe ne sert plus que de repli.
                if (target is Component component)
                {
                    string named = Navigation.Scanner.Describe(component, allowGenericName: false);
                    if (!string.IsNullOrWhiteSpace(named)) return named;
                }

                InteractionInfo info = target?.InteractionPoint;
                if (info?.interactionText != null && info.interactionText.Count > 0)
                {
                    int idx = Mathf.Clamp(first.interactionType, 0, info.interactionText.Count - 1);
                    // Le texte d'interaction n'est pas toujours localisé : plusieurs classes du jeu
                    // le codent en dur en anglais (`Decoration.InteractionPoint` renvoie « Open »).
                    // On le passe donc au traducteur, qui laisse intact ce qu'il connaît déjà en
                    // français et couvre ces quelques verbes restés en anglais.
                    string text = UiNameTranslator.Translate(TextUtil.Clean(info.interactionText[idx]));
                    if (!string.IsNullOrWhiteSpace(text)) return text;
                }
            }

            return DescribeObstacle(player, facing);
        }

        /// <summary>
        /// Nom de la tuile de sol sous la case en face (herbe, sable, pierre, chemin...) et
        /// présence d'eau. Premier essai (22/08/2026) ne renvoyait jamais rien : la cause était
        /// `Tilemap.WorldToCell(worldPos)`, qui utilise la transformation de grille standard
        /// d'Unity — alors qu'en décompilant `Wish.GameManager` (méthodes `GetBottomTile`/
        /// `IsBottomTileType`), le jeu indexe TOUJOURS ses tuilemaps directement par coordonnée
        /// de case entière (`new Vector3Int(position.x, position.y, 0)`, la même que
        /// `Player.Position`), jamais via WorldToCell. ATTENTION : `Player.Position` n'est PAS une
        /// simple troncature des coordonnées monde, contrairement à ce qui était écrit ici avant —
        /// le monde est isométrique (Y divisé par 1,4142135). Voir Util/TileGeometry.cs, qui porte
        /// désormais la conversion exacte, tirée de la définition du jeu. Corrigé pour faire pareil. On utilise
        /// aussi `GameManager.Instance.TileMaps` (la liste DÉJÀ filtrée par le jeu : elle exclut
        /// `dataLayer`/`topLayer`/les tuilemaps d'ennemis) au lieu de chercher nous-mêmes tous
        /// les Tilemap de la scène.
        /// En complément, `GameManager.Instance.dataLayer` porte une couche de métadonnées
        /// invisible (`Wish.DataTile`, jamais affichée à l'écran mais toujours interrogeable) où
        /// `waterType` (None/Water/FishableWater) est fiable à 100% (une valeur d'enum du jeu,
        /// pas un nom d'objet deviné) : utilisé en priorité pour signaler l'eau, plus important
        /// à savoir pour un joueur aveugle qu'un simple nom de texture.
        /// </summary>
        public static string DescribeGroundTile(Vector2Int tile)
        {
            try
            {
                var cell = new Vector3Int(tile.x, tile.y, 0);

                GameManager gameManager = SingletonBehaviour<GameManager>.Instance;
                if (gameManager == null) return null;

                string water = DescribeWater(gameManager, cell);

                string groundName = null;
                foreach (Tilemap tilemap in gameManager.TileMaps)
                {
                    if (tilemap == null) continue;
                    TileBase tileAsset = tilemap.GetTile(cell);
                    if (tileAsset == null) continue;

                    // La NATURE du sol, telle que le jeu la connaît lui-même.
                    //
                    // `SunHavenTile.footstepType` est ce qui décide du bruit de pas : terre,
                    // herbe, pierre, bois, sable, neige, eau peu profonde. C'est une valeur
                    // d'énumération, donc juste par construction — là où lire le nom de la
                    // texture ne donnait un résultat que si ce nom figurait dans un dictionnaire,
                    // et « rien devant vous » le reste du temps. Signalé en jeu : « je veux savoir
                    // ce qui est devant moi comme sol ».
                    if (tileAsset is SunHavenTile havenTile)
                    {
                        groundName = FootstepName(havenTile.footstepType);
                        if (!string.IsNullOrWhiteSpace(groundName)) break;
                    }

                    // Repli : le nom de la texture, quand il est reconnu. TranslateTerrain renvoie
                    // null si aucun mot ne l'est, plutôt que de lâcher un nom d'asset anglais —
                    // le terrain étant décrit à chaque pas, un seul nom brut s'entendrait des
                    // dizaines de fois par minute.
                    string translated = UiNameTranslator.TranslateTerrain(tileAsset.name);
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

        /// <summary>
        /// Le nom du type de sol. Sept valeurs, celles dont le jeu se sert pour ses bruits de pas.
        /// </summary>
        private static string FootstepName(FootstepType type)
        {
            switch (type)
            {
                case FootstepType.Dirt:         return Localization.Language.T("terre", "dirt");
                case FootstepType.Grass:        return Localization.Language.T("herbe", "grass");
                case FootstepType.Stone:        return Localization.Language.T("pierre", "stone");
                case FootstepType.Wood:         return Localization.Language.T("bois", "wood");
                case FootstepType.Sand:         return Localization.Language.T("sable", "sand");
                case FootstepType.Snow:         return Localization.Language.T("neige", "snow");
                case FootstepType.ShallowWater: return Localization.Language.T("eau peu profonde", "shallow water");
                default:                        return null;
            }
        }

        private static string DescribeWater(GameManager gameManager, Vector3Int cell)
        {
            if (gameManager.dataLayer == null) return null;
            DataTile dataTile = gameManager.dataLayer.GetTile<DataTile>(cell);
            if (dataTile == null) return null;
            return dataTile.waterType switch
            {
                WaterType.FishableWater => Localization.Language.T("eau où l'on peut pêcher", "fishable water"),
                WaterType.Water => Localization.Language.T("eau", "water"),
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
            // QUELLE culture, et pas seulement « une culture ».
            //
            // Sans la vue, « Culture, arrosée, prête à récolter » ne dit pas si l'on est devant du
            // blé ou une citrouille — donc ni ce qu'on va récolter, ni si l'on est au bon endroit.
            // `Crop._cropItem` porte l'objet récolté, dont le nom est déjà traduit par le jeu.
            string kind = CropName(crop);

            if (crop.data == null)
                return kind ?? Localization.Language.T("Culture.", "Crop.");

            if (crop.data.dead)
                return kind != null
                    ? Localization.Language.T($"{kind}, morte.", $"{kind}, dead.")
                    : Localization.Language.T("Culture morte.", "Dead crop.");

            string watered = Localization.Language.T(
                crop.data.watered ? "arrosée" : "non arrosée",
                crop.data.watered ? "watered" : "not watered");

            int days = Mathf.Max(crop.DaysLeft, 0);
            string growth = crop.CheckGrowth
                ? Localization.Language.T("prête à récolter", "ready to harvest")
                : Localization.Language.T(
                    $"encore {days} jour{(days > 1 ? "s" : "")} avant maturité",
                    $"{days} more day{(days > 1 ? "s" : "")} to ripen");

            string head = kind ?? Localization.Language.T("Culture", "Crop");
            return $"{head}, {watered}, {growth}.";
        }

        /// <summary>
        /// Le nom de la culture, tel que le jeu l'appelle. Null si on ne peut pas le lire, auquel
        /// cas l'appelant retombe sur le mot générique plutôt que sur une invention.
        /// </summary>
        private static string CropName(Crop crop)
        {
            try
            {
                string name = TextUtil.Clean(crop._cropItem?.UnformattedDisplayName);
                return string.IsNullOrWhiteSpace(name) ? null : name;
            }
            catch { return null; }
        }

        /// <summary>
        /// Quand rien n'est planté sur la case en face, on regarde quand même si c'est une
        /// case de terre cultivable/labourée/arrosée, pour l'annoncer comme le ferait
        /// stardew-access (au lieu de dire "rien devant vous" sur une case de plantation vide).
        /// </summary>
        public static string DescribeFarmland(Vector2Int tile)
        {
            try
            {
                FarmingTileInfo info = SingletonBehaviour<GameSave>.Instance.GetFarmingInfo(
                    tile, ScenePortalManager.ActiveSceneIndex);

                return info switch
                {
                    FarmingTileInfo.Farmable => Localization.Language.T(
                        "Terre cultivable, pas encore labourée.", "Farmable soil, not tilled yet."),
                    FarmingTileInfo.Hoed => Localization.Language.T(
                        "Terre labourée, pas arrosée.", "Tilled soil, not watered."),
                    FarmingTileInfo.Watered => Localization.Language.T(
                        "Terre labourée et arrosée, rien de planté.", "Tilled and watered soil, nothing planted."),
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
                Vector2Int frontTile = TileGeometry.WorldToTile(frontPos);

                // On ne garde que ce qui est RÉELLEMENT sur la case visée, et on nomme le plus
                // proche.
                //
                // Signalé en jeu : une pierre à gauche, une pierre à droite, et l'annonce disait
                // « Ennemi, bloque le passage ». Deux causes se cumulaient. Le cercle de détection
                // déborde sur les cases voisines — le monde est isométrique, donc un rayon en
                // unités de monde ne correspond pas à une case — et `OverlapCircleAll` rend ses
                // résultats dans un ordre ARBITRAIRE : on nommait donc le premier venu du
                // voisinage, pas ce qui barre le passage.
                //
                // Filtrer par case supprime les voisins ; trier par distance rend l'annonce
                // déterministe, au lieu de dépendre de l'ordre où le moteur a rangé ses objets.
                var nearby = Physics2D.OverlapCircleAll(frontPos, 0.45f)
                    .Where(h => h != null && !h.isTrigger)
                    .OrderBy(h => Vector2.Distance(h.ClosestPoint(frontPos), frontPos))
                    .ToArray();

                var onTile = nearby
                    .Where(h => TileGeometry.WorldToTile(h.ClosestPoint(frontPos)) == frontTile)
                    .ToArray();

                // LE SILENCE ÉTAIT PIRE QUE L'APPROXIMATION.
                //
                // Le filtre par case écarte bien les voisins, mais il écartait aussi ce qui barre
                // vraiment le passage : un arbre porte son collision au pied du tronc, et le point
                // le plus proche tombe souvent juste de l'autre côté d'une frontière de case — le
                // monde est isométrique, les arrondis ne pardonnent pas. On se retrouvait bloqué
                // devant quelque chose dont le mod disait « rien devant vous ». Signalé en jeu.
                //
                // On garde donc la précision quand elle donne un résultat, et on retombe sur le
                // plus proche des alentours quand elle n'en donne aucun. Nommer approximativement
                // ce qui bloque vaut infiniment mieux que se taire devant un mur.
                var hits = onTile.Length > 0 ? onTile : nearby;

                foreach (Collider2D hit in hits)
                {
                    if (hit.attachedRigidbody != null && hit.attachedRigidbody.gameObject == player.gameObject) continue;
                    if (hit.transform.IsChildOf(player.transform)) continue;

                    // Nommer l'obstacle plutôt que signaler qu'il y en a un.
                    //
                    // Signalé en jeu : « quelque chose bloque votre passage, toujours sans savoir
                    // quoi ». Un obstacle sans nom n'apprend rien — on sait déjà qu'on est bloqué,
                    // puisqu'on n'avance pas. Ce qu'on veut savoir, c'est si c'est un arbre à
                    // abattre, un rocher à miner, un meuble à déplacer ou un mur à contourner.
                    // On ne rend que le NOM, sans direction ni ponctuation : l'appelant assemble
                    // la phrase avec le reste de ce que porte la case, et y ajoute la direction
                    // une seule fois.
                    string name = NameOfObstacle(hit);
                    if (!string.IsNullOrWhiteSpace(name))
                        return Localization.Language.T($"{name}, bloque le passage",
                                                       $"{name}, blocking the way");

                    return Localization.Language.T("quelque chose bloque le passage",
                                                   "something is blocking the way");
                }
            }
            catch
            {
                // Détection best-effort : en cas de souci, on se contente du message par défaut.
            }
            return null;
        }

        /// <summary>
        /// Le nom de ce qui bloque, cherché d'abord sur les composants du jeu.
        ///
        /// On réemploie le descripteur du scanner : il sait déjà nommer un rocher, un arbre, un
        /// personnage, un meuble ou un portail, et une seconde façon de nommer les mêmes choses
        /// finirait par diverger de la première. Le collisionneur est souvent porté par un enfant,
        /// d'où la remontée vers le parent avant d'abandonner.
        /// </summary>
        private static string NameOfObstacle(Collider2D hit)
        {
            try
            {
                // On reste SUR l'objet touché, et au plus sur son parent direct.
                //
                // Remonter toute la hiérarchie, comme je le faisais, laissait n'importe quel
                // ancêtre lointain donner son identité à l'obstacle : une clôture rangée sous le
                // conteneur d'une créature s'annonçait « Enemy », alors qu'aucun ennemi ne barrait
                // le passage. Un obstacle est ce qu'on heurte, pas ce sous quoi il est rangé. Le
                // parent direct reste admis parce que le collisionneur est souvent porté par un
                // enfant technique de l'objet réel.
                var components = new List<Component>();
                components.AddRange(hit.GetComponents<Component>());
                if (hit.transform.parent != null)
                    components.AddRange(hit.transform.parent.GetComponents<Component>());

                foreach (Component c in components)
                {
                    if (c == null || c is Transform || c is Collider2D) continue;

                    string named = Navigation.Scanner.Describe(c, allowGenericName: false);
                    if (!string.IsNullOrWhiteSpace(named)) return named;
                }

                // Aucun composant connu : on accepte alors le nom technique mis en mots, qui vaut
                // toujours mieux que « quelque chose ».
                foreach (Component c in components)
                {
                    if (c == null || c is Transform || c is Collider2D) continue;

                    string described = Navigation.Scanner.Describe(c);
                    if (!string.IsNullOrWhiteSpace(described)) return described;
                }
            }
            catch { }

            // Dernier repli : le nom technique de l'objet, mis en mots. Il ne dit pas tout, mais
            // « Clôture » vaut infiniment mieux que « quelque chose ».
            try { return UiNameTranslator.TranslateTerrain(hit.gameObject.name); }
            catch { return null; }
        }
    }
}
