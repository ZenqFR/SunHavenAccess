using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using I2.Loc;
using Wish;
using SunHavenAccess.Speech;
using SunHavenAccess.Util;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Festivals et anniversaires du calendrier (`Wish.CalendarUI`/`SeasonEventData`/
    /// `SeasonalEventInfo`, tous champs publics une fois `CalendarUI` trouvée). L'écran natif
    /// (`Wish.CalendarUI`) est une grille de jours purement visuelle sans aucune interaction
    /// clavier (pas de Button/Selectable/ISelectHandler nulle part dans sa décompilation,
    /// contrairement à la carte du monde ou l'arbre de compétences) — rien à naviguer, juste à
    /// lire. `seasonEventDatas` (Dictionary&lt;Season, SeasonEventData&gt;) est un champ PRIVÉ
    /// d'instance sur le CalendarUI actuellement chargé en scène : lu par réflexion une fois
    /// l'instance trouvée via `Object.FindObjectOfType`.
    /// </summary>
    public static class FestivalAnnouncer
    {
        private static FieldInfo _seasonEventDatasField;

        public static void AnnounceThisSeason()
        {
            CalendarUI calendar = UnityEngine.Object.FindObjectOfType<CalendarUI>();
            DayCycle dayCycle = DayCycle.Instance;
            if (calendar == null || dayCycle == null)
            {
                TolkSpeech.Speak("Le calendrier n'est pas disponible pour le moment.", true);
                return;
            }

            // DayCycle.MonthDay (STATIQUE, comme DayCycle.Weekday/Year utilisés dans
            // ClockAnnouncer) est le jour DANS la saison actuelle, 1-based — c'est à ça que
            // SeasonalEventInfo.day fait référence (confirmé dans CalendarUI.GenerateCalendarUI,
            // qui compare exactement ces deux valeurs). DayCycle.Day (aussi statique) est un
            // compteur global de jours écoulés, pas ce qu'il faut ici.
            int currentMonthDay = DayCycle.MonthDay;

            _seasonEventDatasField ??= typeof(CalendarUI).GetField("seasonEventDatas", BindingFlags.NonPublic | BindingFlags.Instance);
            if (_seasonEventDatasField?.GetValue(calendar) is not Dictionary<Season, SeasonEventData> seasonEventDatas
                || !seasonEventDatas.TryGetValue(dayCycle.Season, out SeasonEventData data)
                || data?.seasonalEvents == null)
            {
                TolkSpeech.Speak("Aucune information de festival disponible pour cette saison.", true);
                return;
            }

            List<SeasonalEventInfo> events = data.seasonalEvents
                .Where(e => e != null && e.eventType == Wish.EventType.Event)
                .OrderBy(e => e.day)
                .ToList();

            if (events.Count == 0)
            {
                TolkSpeech.Speak("Aucun festival prévu cette saison.", true);
                return;
            }

            var parts = new List<string>();
            foreach (SeasonalEventInfo evt in events)
            {
                string name = TextUtil.Clean(LocalizeText.TranslateText(evt.keyName, evt.name));
                string description = TextUtil.Clean(LocalizeText.TranslateText(evt.keyDescription, evt.description));
                string when = evt.day == currentMonthDay
                    ? Localization.Language.T("aujourd'hui", "today")
                    : evt.day > currentMonthDay
                        ? Localization.Language.T($"jour {evt.day}", $"day {evt.day}")
                        : Localization.Language.T($"jour {evt.day} (déjà passé)", $"day {evt.day} (already past)");

                string sentence = $"{name}, {when}";
                if (!string.IsNullOrWhiteSpace(description))
                    sentence += Localization.Language.T($" : {description}", $": {description}");
                parts.Add(sentence + ".");
            }

            TolkSpeech.Speak(string.Join(" ", parts), true);
        }
    }
}
