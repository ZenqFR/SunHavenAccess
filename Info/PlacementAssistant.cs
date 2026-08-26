using HarmonyLib;
using UnityEngine;
using Wish;
using SunHavenAccess.Cursor;
using SunHavenAccess.Navigation;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Assistance au placement de meubles et de bâtiments.
    ///
    /// Poser un objet dans Sun Haven est une action purement visuelle : un aperçu suit la souris
    /// et change de teinte — blanc si l'emplacement convient, rouge sinon. Sans la vue, on ne
    /// sait ni où vise l'aperçu, ni pourquoi le clic ne fait rien. C'est resté jusqu'ici l'un des
    /// derniers pans du jeu totalement fermés.
    ///
    /// Le principe retenu est de NE PAS reprogrammer le placement. Le jeu recalcule à chaque
    /// frame, dans `Placeable.LateUpdate`, la case visée et sa validité ; ce fichier se contente
    /// de les lire et de les dire. Deux conséquences heureuses : ce qui est annoncé est
    /// exactement ce que le jeu va faire (aucune règle dupliquée, donc aucune divergence
    /// possible), et toutes les variantes de placeables — maisons, granges, arbres, papier
    /// peint — sont couvertes sans code spécifique.
    ///
    /// Pour VISER, on réutilise le curseur libre : tant qu'un placeable est en main, la souris
    /// est repointée sur la case du curseur à chaque frame, et le jeu suit de lui-même puisqu'il
    /// lit la position de la souris. Le clavier pilote ainsi une mécanique conçue à la souris,
    /// sans qu'aucune ligne du jeu n'ait été détournée.
    /// </summary>
    public static class PlacementAssistant
    {
        /// <summary>
        /// Les tailles de décoration sont exprimées en sixièmes de case dans tout le jeu
        /// (`Decoration.Size`, `roundedMousePos`, `SetObjectSubTile`…). Une case fait 6 unités.
        /// </summary>
        private const int SubTilesPerTile = 6;

        // Accès aux champs internes de Placeable. `canBePlaced` est protected et
        // `roundedMousePos` private : ce sont pourtant les deux seules valeurs qui décrivent
        // l'intention du joueur. On les lit, on n'y écrit jamais.
        //
        // Résolus dans un constructeur statique protégé plutôt qu'en initialiseur de champ : si
        // une mise à jour du jeu renommait l'un de ces champs, un initialiseur qui lève laisserait
        // la classe définitivement inutilisable et ferait journaliser une exception à chaque
        // frame. Ici l'échec se solde par un module silencieux et un seul avertissement.
        private static readonly AccessTools.FieldRef<Placeable, bool> CanBePlacedRef;
        private static readonly AccessTools.FieldRef<Placeable, Vector2Int> RoundedMouseRef;

        static PlacementAssistant()
        {
            try
            {
                CanBePlacedRef = AccessTools.FieldRefAccess<Placeable, bool>("canBePlaced");
                RoundedMouseRef = AccessTools.FieldRefAccess<Placeable, Vector2Int>("roundedMousePos");
            }
            catch (System.Exception e)
            {
                Plugin.Log?.LogWarning(
                    "Placement : champs internes de Placeable introuvables, l'assistance au " +
                    "placement sera inactive. " + e.Message);
            }
        }

        private static Placeable _current;
        private static bool _lastValid;
        private static bool _hasReported;
        private static Vector2Int _lastTile;

        /// <summary>Un objet posable est-il actuellement en main ?</summary>
        public static bool Active => _current != null;

        public static void Tick()
        {
            if (CanBePlacedRef == null) return;

            Player player = Player.Instance;
            var placeable = player?.UseItem as Placeable;

            if (placeable == null)
            {
                if (_current != null) EndPlacementMode();
                return;
            }

            if (placeable != _current)
            {
                BeginPlacementMode(placeable);
                return;
            }

            // Le curseur libre sert de main : la souris le suit, donc l'aperçu du jeu aussi.
            // Repointé à chaque frame et non au seul déplacement du curseur, car le jeu relit la
            // souris en continu — un pointage unique serait effacé par le moindre mouvement réel.
            if (FreeTileCursor.Active)
            {
                MouseCursor.ExternalControl = true;
                FreeTileCursor.PointMouseAtCursor();
            }
            else
            {
                MouseCursor.ExternalControl = false;
            }

            AnnounceChanges(placeable);
        }

        private static void BeginPlacementMode(Placeable placeable)
        {
            _current = placeable;
            _hasReported = false;

            string name = DescribeName(placeable);
            string footprint = DescribeFootprint(placeable);

            string aiming = FreeTileCursor.Active
                ? "Le curseur libre vise pour vous."
                : "Activez le curseur libre pour viser au clavier.";

            TolkSpeech.Speak($"Mode placement : {name}{footprint}. {aiming}", true);
        }

        private static void EndPlacementMode()
        {
            _current = null;
            _hasReported = false;
            // Le pointeur revient à son propriétaire habituel, sans quoi le curseur souris
            // directionnel resterait muselé longtemps après la fin du placement.
            MouseCursor.ExternalControl = false;
            TolkSpeech.Speak("Mode placement quitté.", true);
        }

        /// <summary>
        /// Annonce la validité de l'emplacement, et UNIQUEMENT quand elle bascule.
        ///
        /// Deux règles s'imposent ici, apprises à l'usage. D'abord ne rien dire tant que rien ne
        /// change : le jeu recalcule soixante fois par seconde, et répéter « emplacement valide »
        /// à ce rythme noierait tout le reste. Ensuite ne pas répéter à chaque changement de
        /// case : balayer six cases invalides doit s'entendre une fois, pas six. C'est d'ailleurs
        /// ce que perçoit un joueur voyant — la teinte de l'aperçu ne l'alerte que lorsqu'elle
        /// change.
        ///
        /// L'annonce ne coupe jamais la parole en cours : le curseur libre vient de décrire la
        /// case, et cette description est la plus utile des deux. La validité se met à la suite.
        /// </summary>
        private static void AnnounceChanges(Placeable placeable)
        {
            bool valid;
            Vector2Int tile;

            try
            {
                valid = CanBePlacedRef(placeable);
                tile = ToTile(RoundedMouseRef(placeable));
            }
            catch
            {
                // Un placeable dont les champs ne sont pas encore initialisés : rien à dire.
                return;
            }

            if (_hasReported && valid == _lastValid && tile == _lastTile) return;

            bool validityChanged = valid != _lastValid;
            _lastTile = tile;
            _lastValid = valid;

            // À la toute première frame, les champs valent encore leur valeur par défaut : on
            // enregistre l'état sans le dire, pour ne pas annoncer un « invalide » qui n'est
            // qu'un champ non initialisé.
            if (!_hasReported)
            {
                _hasReported = true;
                return;
            }

            if (!validityChanged) return;

            TolkSpeech.Speak(valid ? "Emplacement valide." : "Emplacement invalide.", false);
        }

        /// <summary>
        /// État complet du placement, à la demande. Les annonces automatiques ne signalent que
        /// les bascules ; il faut pouvoir redemander où l'on en est sans avoir à bouger l'objet.
        /// </summary>
        public static void AnnounceStatus()
        {
            if (_current == null)
            {
                TolkSpeech.Speak("Aucun objet à poser en main.", true);
                return;
            }

            string name = DescribeName(_current);
            string footprint = DescribeFootprint(_current);
            string state;

            try
            {
                state = CanBePlacedRef(_current) ? "emplacement valide" : "emplacement invalide";
            }
            catch
            {
                state = "état inconnu";
            }

            string aiming = FreeTileCursor.Active
                ? "Visée pilotée par le curseur libre."
                : "Curseur libre inactif : la visée suit la souris.";

            TolkSpeech.Speak($"{name}{footprint}, {state}. {aiming}", true);
        }

        /// <summary>Convertit une position en sixièmes de case vers des coordonnées de cases.</summary>
        private static Vector2Int ToTile(Vector2Int subTile) =>
            new Vector2Int(
                Mathf.FloorToInt((float)subTile.x / SubTilesPerTile),
                Mathf.FloorToInt((float)subTile.y / SubTilesPerTile));

        private static string DescribeName(Placeable placeable)
        {
            if (!string.IsNullOrWhiteSpace(placeable.decorationName)) return placeable.decorationName;

            Decoration decoration = placeable.Decoration;
            if (decoration != null && !string.IsNullOrWhiteSpace(decoration.decorationName))
                return decoration.decorationName;

            return "objet";
        }

        /// <summary>
        /// Emprise au sol, en cases. Passée sous silence pour un objet d'une seule case : la
        /// mentionner à chaque fois noierait l'information utile pour les gros bâtiments.
        /// </summary>
        private static string DescribeFootprint(Placeable placeable)
        {
            Decoration decoration = placeable.Decoration;
            if (decoration == null) return string.Empty;

            Vector2Int size = decoration.Size;
            int width = Mathf.Max(1, Mathf.RoundToInt((float)size.x / SubTilesPerTile));
            int height = Mathf.Max(1, Mathf.RoundToInt((float)size.y / SubTilesPerTile));

            if (width <= 1 && height <= 1) return string.Empty;
            return $", emprise {width} sur {height} cases";
        }
    }
}
