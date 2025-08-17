using System;
using System.Linq;
using System.Text.RegularExpressions;
using ApiServices;
using DbServices;

namespace DataLayerServices
{
    public static class DtoMapper
    {
        public static DbRaceSummary MapToDbRace(RaceSummary apiModel, string category)
        {
            return new DbRaceSummary
            {
                Season = Convert.ToInt32(apiModel.Season),
                Race = apiModel.Race,
                TrackName = apiModel.TrackName,
                Downloaded = DateTime.Today,
                ApiRequestsRemaining = apiModel.ApiRequestsRemaining,
                Managers = apiModel.Managers.Select(m => new DbManager
                {
                    idx = Convert.ToInt32(m.idx),
                    natCode = m.natCode,
                    idm = m.idm,
                    Name = m.Name,
                    Pos = m.Pos,
                    Progress = m.Progress,
                    Tyre = m.Tyre,
                    RaceTime = m.RaceTime,
                    LapsCompleted = m.LapsCompleted,
                    PitStops = m.PitStops,
                    BestLapTime = m.BestLapTime,
                    AvgSpeed = m.AvgSpeed,
                    BestPitTime = m.BestPitTime,
                    Points = 0,
                    TotPoints = 0,
                    Category = category
                }).ToList()
            };
        }
        public static DbCalendar MapToDbCalendar(Calendar calendar)
        {
            return new DbCalendar
            {
                Events = calendar.Events.Select(m => new DbEvent
                {
                    DateEvent = ConvertToDateTime(m.DateEvent),
                    Idx = m.Idx,
                    EventType = m.EventType
                }).ToList()
            };
        }
        public static DbOffice MapToDbOffice(Office apiOffice)
        {
            return new DbOffice
            {
                ApiRequestsRemaining = apiOffice.ApiRequestsRemaining,
                EndOfSeason = apiOffice.EndOfSeason,
                NextSeasonResetDate = ConvertToDateTimeForOffice(apiOffice.NextSeasonResetDate),
                Progress = apiOffice.Progress,
                RaceNb = apiOffice.RaceNb,
                SeasonNb = apiOffice.SeasonNb,
                TrackId = apiOffice.TrackId,
                UpdateInProgress = apiOffice.UpdateInProgress
            };
        }
        private static DateTime ConvertToDateTimeForOffice(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                throw new ArgumentException("La stringa della data non può essere vuota", nameof(dateString));

            string cleanedDate = Regex.Replace(dateString, @"(\d+)(st|nd|rd|th)\b", "$1");
            string fullDateString = $"{cleanedDate} {DateTime.Now.Year}";
            string format = "MMM dd yyyy";

            return DateTime.ParseExact(
                fullDateString,
                format,
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
        private static DateTime ConvertToDateTime(string dateString)
        {
            string textContent = Regex.Replace(
                dateString,
                @"<[^>]+>|&nbsp;",
                string.Empty
            ).Trim();

            if (string.Equals(textContent, "Today", StringComparison.OrdinalIgnoreCase))
                return DateTime.Today;

            if (string.Equals(textContent, "YesterDay", StringComparison.OrdinalIgnoreCase))
                return DateTime.Today.AddDays(-1);

            string cleanedDate = Regex.Replace(
                textContent,
                @"(\d+)(st|nd|rd|th)",
                "$1"
            );

            return DateTime.ParseExact(
                cleanedDate,
                "MMM d, yyyy",
                System.Globalization.CultureInfo.InvariantCulture
            );
        }
    }
}
