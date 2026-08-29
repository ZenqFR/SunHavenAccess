using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Navigation
{
    /// <summary>
    /// Carte du monde (`Wish.Map`) : chaque lieu (`Wish.LocationName`) répond à la sélection et
    /// au clic (ISelectHandler/ISubmitHandler/IPointerClickHandler) mais N'HÉRITE PAS de
    /// `UnityEngine.UI.Selectable` — invisible pour le scan générique de MenuNavigator
    /// (`Object.FindObjectsOfType&lt;Selectable&gt;()`), donc injoignable au clavier sans ce
    /// système dédié, même si le reste de l'écran (boutons fermer/changer de région, de vrais
    /// Selectable) fonctionne déjà via MenuNavigator. Contredit ce que le README supposait
    /// ("probablement déjà accessible") — vérifié en décompilation, pas juste en jeu.
    ///
    /// `MapImage.OpenLocation()` (publique) fait tout le travail utile en un seul appel : elle
    /// centre/surligne la carte sur le lieu ET remplit `Map.locationDescriptionTMP` — un Harmony
    /// postfix sur `Map.OpenLocation(string, LocationName)` (voir Patches/MapPatch.cs) annonce
    /// alors nom + description. Cette classe se contente de choisir QUEL lieu ouvrir : région
    /// actuelle (`Map.townType`, privé) et sa liste de lieux (5 champs privés séparés, un par
    /// région) lus par réflexion, filtrés aux lieux réellement visibles/débloqués à l'écran.
    /// </summary>
    public static class MapNavigator
    {
        private static int _index = -1;
        private static List<MapImage> _cached = new List<MapImage>();

        private static FieldInfo _townTypeField;
        private static readonly Dictionary<TownType, string> RegionFieldNames = new Dictionary<TownType, string>
        {
            { TownType.SunHaven, "sunHavenMapImages" },
            { TownType.Nelvari, "nelvariMapImages" },
            { TownType.Withergate, "withergateMapImages" },
            { TownType.BrinestoneDeeps, "brinestoneMapImages" },
            { TownType.GreatCity, "greatCityMapImages" },
        };
        private static readonly Dictionary<TownType, FieldInfo> RegionFields = new Dictionary<TownType, FieldInfo>();

        public static void AnnounceNext() => Cycle(1);
        public static void AnnouncePrevious() => Cycle(-1);

        /// <summary>
        /// La carte en LISTE : tous les lieux d'un coup, plutôt qu'un cycle où l'on ne sait jamais
        /// combien il en reste ni où l'on en est.
        ///
        /// Valider un lieu l'ouvre exactement comme un clic : la carte se centre dessus et le jeu
        /// annonce sa description.
        ///
        /// Valider un lieu lance aussi le trajet à pied vers lui — quand il y mène quelque part.
        ///
        /// Attention au piège : `MapImage.GoToLocation()` porte un nom qui ment. Elle ne fait que
        /// recentrer la VUE de la carte sur l'icône, elle ne déplace jamais le personnage. Et la
        /// position de l'icône est une position d'écran : marcher vers elle enverrait vers une
        /// coordonnée arbitraire du monde.
        ///
        /// Ce qui existe vraiment, c'est l'ENTRÉE du bâtiment, un objet du monde avec une vraie
        /// position, que le scanner sait déjà trouver. On rapproche le lieu de son entrée par le
        /// nom de scène, et c'est vers elle qu'on marche. Un lieu situé dans une autre zone n'a
        /// pas d'entrée ici : on le dit, plutôt que de faire semblant.
        /// </summary>
        public static void OpenList()
        {
            Map map = UIHandler.Instance != null ? UIHandler.Instance.map : null;
            if (map == null || !map.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("La carte n'est pas ouverte.", true);
                return;
            }

            List<MapImage> visible = GetVisibleLocations(map);
            if (visible.Count == 0)
            {
                TolkSpeech.Speak("Aucun lieu trouvé sur cette carte.", true);
                return;
            }

            // Toute ouverture de la carte enrichit le plan des liaisons : on est forcément dans une
            // zone, ses sorties sont lisibles maintenant, et jamais gratuitement plus tard.
            WorldLinks.Learn();

            var labels = visible.Select(NameOf).ToList();
            Menus.ListMenu.Open("Lieux de la carte", labels, chosen => Choose(visible, chosen));
        }

        /// <summary>
        /// Le nom du lieu, tel que le jeu l'écrit dans la langue où l'on joue.
        ///
        /// On lisait auparavant TOUS les textes de l'icône, ce qui donnait « carte popup café » :
        /// le nom du lieu noyé dans les noms techniques des objets qui l'entourent. Or `MapImage`
        /// porte sa propre clé de traduction, et `LocalizeText.TranslateText` rend le nom seul,
        /// déjà traduit par le jeu. Même principe que pour les objets : la source du jeu plutôt
        /// qu'un ramassage de texte à l'écran.
        /// </summary>
        private static string NameOf(MapImage image)
        {
            try
            {
                string translated = LocalizeText.TranslateText(image.locationKey, image.location);
                if (!string.IsNullOrWhiteSpace(translated)) return Util.TextUtil.Clean(translated);

                if (!string.IsNullOrWhiteSpace(image.location)) return Util.TextUtil.Clean(image.location);
            }
            catch { }
            return "Lieu sans nom";
        }

        /// <summary>
        /// Choisir un lieu ouvre ce qu'on peut EN FAIRE, plutôt que d'agir à sa place.
        ///
        /// Valider lançait directement le trajet, quand il y en avait un, en plus d'ouvrir la
        /// description : deux choses d'un coup, dont une invisible et jamais demandée. Signalé en
        /// jeu — « faudrait mettre une ligne supplémentaire pour faire y aller ». C'est juste : un
        /// lieu qu'on consulte et un lieu où l'on se rend sont deux intentions différentes, et
        /// c'est à celui qui écoute de trancher, pas au mod de deviner.
        ///
        /// « S'y rendre à pied » vient EN PREMIER : c'est le geste qu'on vient chercher. La
        /// description reste à un cran, et Ctrl+haut ramène à la liste des lieux — on ressort donc
        /// d'un lieu sans avoir à rouvrir la carte.
        /// </summary>
        private static void Choose(List<MapImage> visible, int index)
        {
            if (index < 0 || index >= visible.Count) return;

            MapImage location = visible[index];
            if (location == null) return;

            string label = NameOf(location);
            Component entrance = EntranceFor(location);

            // La zone du lieu, si on y est déjà allé une fois. C'est ce qui permet un trajet
            // complet depuis n'importe où plutôt qu'un simple « pas d'entrée ici ».
            string targetScene = entrance != null
                ? Scanner.PortalDestination(entrance as ScenePortalSpot)
                : WorldLinks.FindScene(location.location) ?? WorldLinks.FindScene(label);

            bool reachable = entrance != null || targetScene != null;

            var actions = new List<string>
            {
                reachable
                    ? Localization.Language.T("S'y rendre", "Go there")
                    : Localization.Language.T("S'y rendre : lieu jamais visité",
                                              "Go there: never visited"),
                Localization.Language.T("Lire la description", "Read the description"),
                Localization.Language.T("Sortir vers une autre zone", "Leave for another area"),
            };

            Menus.ListMenu.Open(label, actions,
                chosen =>
                {
                    if (chosen == 0) { GoTo(entrance, targetScene, label); return; }

                    if (chosen == 2) { OpenExits(); return; }

                    // `OpenLocation` centre la carte ET remplit le panneau de description ; le
                    // patch Harmony sur Map.OpenLocation se charge de l'annoncer.
                    location.OpenLocation();
                },
                onExitUp: OpenList);
        }

        /// <summary>
        /// Se rendre au lieu, d'où que l'on parte.
        ///
        /// Trois cas, du plus simple au plus ambitieux. L'entrée est ici : on marche, c'est fini.
        /// L'entrée est ailleurs mais on connaît le chemin : le trajet traverse les zones tout
        /// seul, et l'on n'a plus rien à faire. On n'y est jamais allé : on le dit, franchement —
        /// le mod ne connaît que ce qui a été exploré, et prétendre le contraire enverrait marcher
        /// au hasard.
        /// </summary>
        private static void GoTo(Component entrance, string targetScene, string label)
        {
            if (entrance != null)
            {
                PathingController.TravelTo(entrance.transform.position, label);
                return;
            }

            if (targetScene != null && Journey.Start(targetScene, label)) return;

            TolkSpeech.Speak(Localization.Language.T(
                $"{label} n'a pas encore été visité : je ne connais pas le chemin. Approchez-vous une première fois, et il sera retenu.",
                $"{label} has not been visited yet: I don't know the way. Get there once, and it will be remembered."), true);
        }

        /// <summary>
        /// L'entrée de ce lieu dans la zone courante, ou null s'il n'y en a pas.
        ///
        /// Un portail sait vers quelle scène il mène ; un lieu de carte porte son nom. On les
        /// rapproche en ignorant espaces, ponctuation et casse, parce que rien ne garantit qu'ils
        /// s'écrivent pareil — et on accepte qu'un nom soit CONTENU dans l'autre, le jeu nommant
        /// ses scènes plus longuement que ses lieux (« CafeInterior » pour « Café »).
        ///
        /// Une correspondance ambiguë — plusieurs entrées possibles — retient la plus proche du
        /// joueur plutôt que d'abandonner : quand deux portes mènent au même endroit, celle d'à
        /// côté est toujours le bon choix.
        ///
        /// L'échec est TRACÉ dans le journal avec le nom cherché et les entrées disponibles :
        /// c'est la seule façon de corriger un rapprochement qu'on ne peut pas voir depuis ici.
        /// </summary>
        private static Component EntranceFor(MapImage location)
        {
            try
            {
                string wanted = Flatten(location.location);
                if (string.IsNullOrEmpty(wanted)) wanted = Flatten(NameOf(location));
                if (string.IsNullOrEmpty(wanted)) return null;

                var portals = Scanner.PortalsInScene().Where(p => p != null).ToList();

                var matches = portals
                    .Select(p => new { Portal = p, Scene = Flatten(Scanner.PortalDestination(p)) })
                    .Where(x => !string.IsNullOrEmpty(x.Scene)
                                && (x.Scene == wanted || x.Scene.Contains(wanted) || wanted.Contains(x.Scene)))
                    .ToList();

                if (matches.Count == 0)
                {
                    Plugin.Log?.LogInfo(
                        $"Carte : aucune entrée pour « {location.location} » (cherché « {wanted} »). " +
                        $"Entrées présentes : {string.Join(", ", portals.Select(Scanner.PortalDestination).Where(s => !string.IsNullOrEmpty(s)).ToArray())}");
                    return null;
                }

                Vector3 from = Wish.Player.Instance != null ? Wish.Player.Instance.transform.position : Vector3.zero;
                return matches
                    .OrderBy(x => Vector3.Distance(x.Portal.transform.position, from))
                    .First().Portal;
            }
            catch { return null; }
        }

        /// <summary>
        /// Les sorties de la zone où l'on est, pour s'approcher d'un lieu qu'on ne peut pas
        /// atteindre d'une traite.
        ///
        /// AUCUN TRAJET NE RELIE DEUX ZONES. Le jeu les charge séparément, et le chemin calculé
        /// s'arrête au bord de celle où l'on se trouve : demander « emmène-moi au café » depuis la
        /// ferme n'a pas de réponse en un seul geste, quoi qu'on fasse. Vérifié dans le journal —
        /// depuis la ferme, les seules entrées existantes mènent au poulailler, à la grange, à la
        /// maison, au champ, à la forêt, à la plage et à la ville. Le café n'est nulle part.
        ///
        /// Mais la ville, elle, est là. On mène donc à la SORTIE, et le reste se fait tout seul :
        /// une fois arrivé, rouvrir la liste des lieux et « s'y rendre à pied » fonctionne, parce
        /// que l'entrée du café existe désormais dans la zone. Deux gestes au lieu d'un impossible.
        ///
        /// Les sorties sont celles que le jeu déclare, pas une liste écrite d'avance : une zone
        /// ajoutée par une mise à jour apparaîtra ici sans qu'on y touche.
        /// </summary>
        private static void OpenExits()
        {
            var exits = Scanner.PortalsInScene()
                .Where(p => p != null && !string.IsNullOrWhiteSpace(Scanner.PortalDestination(p)))
                .OrderBy(p => Wish.Player.Instance != null
                    ? Vector3.Distance(p.transform.position, Wish.Player.Instance.transform.position)
                    : 0f)
                .ToList();

            if (exits.Count == 0)
            {
                TolkSpeech.Speak(Localization.Language.T(
                    "Aucune sortie trouvée dans cette zone.",
                    "No exit found in this area."), true);
                return;
            }

            var labels = exits
                .Select(p => Util.UiNameTranslator.Translate(Scanner.PortalDestination(p)))
                .ToList();

            Menus.ListMenu.Open(
                Localization.Language.T("Sorties de cette zone", "Exits from this area"),
                labels,
                chosen =>
                {
                    if (chosen < 0 || chosen >= exits.Count) return;
                    PathingController.TravelTo(exits[chosen].transform.position, labels[chosen]);
                },
                onExitUp: OpenList);
        }

        private static string Flatten(string s) =>
            new string((s ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static void Cycle(int direction)
        {
            Map map = UIHandler.Instance != null ? UIHandler.Instance.map : null;
            if (map == null || !map.gameObject.activeInHierarchy)
            {
                TolkSpeech.Speak("La carte n'est pas ouverte.", true);
                return;
            }

            List<MapImage> visible = GetVisibleLocations(map);
            if (visible.Count == 0)
            {
                TolkSpeech.Speak("Aucun lieu trouvé sur cette carte.", true);
                return;
            }

            // Si la liste a changé (changement de région) depuis le dernier passage, repartir du début.
            if (!ReferenceEquals(visible, _cached) || _index < 0 || _index >= visible.Count)
            {
                _cached = visible;
                _index = 0;
            }
            else
            {
                _index = ((_index + direction) % visible.Count + visible.Count) % visible.Count;
            }

            MapImage current = visible[_index];
            // Ouvre le lieu comme le ferait un clic : centre/surligne la carte, remplit la
            // description, et déclenche l'annonce vocale via le patch Harmony sur Map.OpenLocation.
            current.OpenLocation();
        }

        private static List<MapImage> GetVisibleLocations(Map map)
        {
            _townTypeField ??= typeof(Map).GetField("townType", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_townTypeField?.GetValue(map) is not TownType townType) return new List<MapImage>();

            if (!RegionFields.TryGetValue(townType, out FieldInfo field))
            {
                if (!RegionFieldNames.TryGetValue(townType, out string fieldName)) return new List<MapImage>();
                field = typeof(Map).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
                RegionFields[townType] = field;
            }

            if (field?.GetValue(map) is not List<MapImage> images) return new List<MapImage>();

            return images
                .Where(mi => mi != null && mi.gameObject.activeInHierarchy)
                .ToList();
        }
    }
}
