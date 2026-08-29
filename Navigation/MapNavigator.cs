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

            var labels = visible.Select(NameOf).ToList();
            Menus.ListMenu.Open("Lieux de la carte", labels, chosen => Choose(visible, chosen));
        }

        private static string NameOf(MapImage image)
        {
            try
            {
                string text = Util.TextUtil.Clean(Util.UiTextExtractor.ExtractAll(image.gameObject));
                if (!string.IsNullOrWhiteSpace(text)) return text;
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

            var actions = new List<string>
            {
                entrance != null
                    ? Localization.Language.T("S'y rendre à pied", "Walk there")
                    : Localization.Language.T("S'y rendre à pied : pas d'entrée dans cette zone",
                                              "Walk there: no entrance in this area"),
                Localization.Language.T("Lire la description", "Read the description"),
            };

            Menus.ListMenu.Open(label, actions,
                chosen =>
                {
                    if (chosen == 0)
                    {
                        if (entrance != null) PathingController.TravelTo(entrance.transform.position, label);
                        else TolkSpeech.Speak(Localization.Language.T(
                            "Ce lieu n'a pas d'entrée dans la zone où vous êtes : il faut d'abord vous en approcher.",
                            "This place has no entrance in the area you are in: you must get closer first."), true);
                        return;
                    }

                    // `OpenLocation` centre la carte ET remplit le panneau de description ; le
                    // patch Harmony sur Map.OpenLocation se charge de l'annoncer.
                    location.OpenLocation();
                },
                onExitUp: OpenList);
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
