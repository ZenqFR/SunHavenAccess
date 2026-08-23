using System;
using System.Collections.Generic;
using I2.Loc;
using Wish;
using SunHavenAccess.Speech;

namespace SunHavenAccess.Info
{
    /// <summary>
    /// Annonce l'heure, le jour, la saison, l'année et la météo actuelle du jeu — rien de tout
    /// ça n'était accessible autrement que visuellement. Réutilise `Wish.DayCycle` (heure/jour/
    /// saison via `Time`/`Day`/`MonthDay`/`Year`/`Season`, drapeaux météo `Raining`/`LightSnow`/
    /// `Heatwave`/`GloomyRain`/`Foggy`/`Windy`) et, pour les noms de saison, le système de
    /// localisation du jeu lui-même (`I2.Loc.ScriptLocalization.Spring`/`Summer`/`Fall`/
    /// `Winter`) plutôt qu'une traduction maison : ça reste donc correct quelle que soit la
    /// langue configurée par le joueur.
    ///
    /// Le jour de la semaine (`DayCycle.Weekday`, 0-6) est maintenant annoncé lui aussi :
    /// correspondance confirmée en extrayant du texte lisible directement dans les données
    /// compilées du jeu (`Sun Haven_Data/data.unity3d`, recherche binaire sur la clé
    /// "Signs.Arena.NoBrawl") — le panneau d'arène dit "reviens Tuesday et Satur[day]" pour le
    /// message "pas de combat aujourd'hui", exactement les jours où le code vérifie `Weekday ==
    /// 2` et `Weekday == 6` (voir Wish.ArenaSign/ArenaNPC en décompilation). Ça correspond très
    /// exactement à la convention .NET/`System.DayOfWeek` (dimanche = 0), utilisée ci-dessous.
    /// </summary>
    public static class ClockAnnouncer
    {
        public static void Announce()
        {
            DayCycle dayCycle = DayCycle.Instance;
            if (dayCycle == null)
            {
                TolkSpeech.Speak("Le jeu n'est pas encore chargé.", true);
                return;
            }

            DateTime time = dayCycle.Time;
            string heure = $"{time.Hour} heure{(time.Hour > 1 ? "s" : "")}" +
                (time.Minute > 0 ? $" {time.Minute:00}" : "");

            string jourSemaine = WeekdayName(DayCycle.Weekday);
            int monthDay = DayCycle.MonthDay;
            int year = DayCycle.Year;
            string saison = SeasonName(dayCycle.Season);

            string meteo = DescribeWeather(dayCycle);

            string jour = jourSemaine != null ? $"{jourSemaine} {monthDay}" : $"jour {monthDay}";
            TolkSpeech.Speak(
                $"Il est {heure}. {jour} de {saison}, année {year}. Météo : {meteo}.",
                true);
        }

        private static string DescribeWeather(DayCycle dayCycle)
        {
            var parts = new List<string>();
            if (dayCycle.GloomyRain) parts.Add("pluie battante");
            else if (dayCycle.Raining) parts.Add("pluie");
            if (dayCycle.LightSnow) parts.Add("légère neige");
            if (dayCycle.Heatwave) parts.Add("canicule");
            if (dayCycle.Foggy) parts.Add("brouillard");
            if (dayCycle.Windy) parts.Add("vent");

            return parts.Count > 0 ? string.Join(", ", parts) : "ciel dégagé";
        }

        /// <summary>
        /// Convention System.DayOfWeek (dimanche = 0), confirmée en décompilation — voir
        /// commentaire de classe. Réutilise les libellés déjà localisés par le jeu.
        /// </summary>
        private static string WeekdayName(int weekday) => weekday switch
        {
            0 => ScriptLocalization.DaySunday,
            1 => ScriptLocalization.DayMonday,
            2 => ScriptLocalization.DayTuesday,
            3 => ScriptLocalization.DayWednesday,
            4 => ScriptLocalization.DayThursday,
            5 => ScriptLocalization.DayFriday,
            6 => ScriptLocalization.DaySaturday,
            _ => null,
        };

        private static string SeasonName(Season season) => season switch
        {
            Season.Spring => ScriptLocalization.Spring,
            Season.Summer => ScriptLocalization.Summer,
            Season.Fall => ScriptLocalization.Fall,
            Season.Winter => ScriptLocalization.Winter,
            _ => season.ToString(),
        };
    }
}
