using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
