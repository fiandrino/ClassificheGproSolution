using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiServices;
using DbServices;

namespace DataLayerServices
{
    public class DataLayer : IDataLayer
    {
        private readonly IApiRequestGpro _api;
        private readonly IDbRepository _db;

        public DataLayer(IApiRequestGpro apiRequestGro, IDbRepository dbRepository)
        {
            this._api = apiRequestGro;
            this._db = dbRepository;
        }

        public async Task<DbOffice> InitializeOffice(string tipoClassifica, string token)
        {
            var dbOffice = await _db.ReadOffice(tipoClassifica.ToString()).ConfigureAwait(false);
            _api.Autenticazione(token);

            var office = await _api.GetApiAsync<Office>(UrlBase.UrlBaseOffice).ConfigureAwait(false);
            dbOffice = DtoMapper.MapToDbOffice(office);
            await _db.ClearOfficeData(tipoClassifica.ToString()).ConfigureAwait(false);
            await _db.SaveOffice(dbOffice, tipoClassifica.ToString());
            return dbOffice;
        }
        public async Task<DbCalendar> InitializeCalendar(string tipoClassifica, string token)
        {
            var dbCalendar = await _db.ReadCalendar(tipoClassifica.ToString()).ConfigureAwait(false);
            if (dbCalendar != null)
            {
                return dbCalendar;
            }
            else
            {
                _api.Autenticazione(token);
                var calendar = await _api.GetApiAsync<Calendar>(UrlBase.UrlBaseCalendar).ConfigureAwait(false);
                calendar = FilterRaceEvents(calendar);
                dbCalendar = DtoMapper.MapToDbCalendar(calendar);
                await _db.SaveCalendar(dbCalendar, tipoClassifica.ToString()).ConfigureAwait(false);
            }

            return dbCalendar;
        }
        public (DateTime? proxRaceDate, DateTime? prevRaceDate) GetRaceDates(List<DbEvent> events)
        {
            var prox = events?
                .Where(d => d.DateEvent >= DateTime.Today)
                .OrderBy(d => d.DateEvent)
                .Select(d => d.DateEvent)
                .FirstOrDefault();

            var prev = events?
                .Where(d => d.DateEvent <= DateTime.Today)
                .OrderByDescending(d => d.DateEvent)
                .Select(d => d.DateEvent)
                .FirstOrDefault();

            return (prox, prev);
        }
        public async Task<DbRaceSummary> GetRaceData(string category, Enums.TipoClassifica tipoClassifica, DateTime? proxRaceDate, DateTime? prevRaceDate, string token)
        {
            var race = await _db.ReadLastRace(category.ToString(), tipoClassifica.ToString());

            if (ShouldFetchNewRace(race, proxRaceDate, prevRaceDate))
            {
                race = await FetchAndProcessNewRace(category, tipoClassifica, proxRaceDate, token);
                await _db.SaveRaceSummary(race, race.Managers, tipoClassifica.ToString());
            }

            return race;
        }
        private bool ShouldFetchNewRace(DbRaceSummary race, DateTime? proxRaceDate, DateTime? prevRaceDate)
        {
            return (race == null || !race.Managers.Any()) || ((proxRaceDate > DateTime.Today && prevRaceDate < DateTime.Today && race.Downloaded < prevRaceDate));
        }
        private async Task<DbRaceSummary> FetchAndProcessNewRace(string category, Enums.TipoClassifica tipoClassifica, DateTime? proxRaceDate, string token)
{
            _api.Autenticazione(token);
            var apiRace = await _api.GetApiAsync<RaceSummary>(UrlBase.urlBaseRaceSummary, category.ToString());

            var race = DtoMapper.MapToDbRace(apiRace, category.ToString());
            race = FiltraPerNatzionalita(race, tipoClassifica);
            race = OrdinaPerPosizioneArrivo(race);

            var puntiManagersStagione = await _db.SommaPuntiGare(
                race.Season,
                category.ToString(),
                tipoClassifica.ToString());

            return AssegnaPunti(race, Enums.PointSystem.Gpro, puntiManagersStagione);
        }
        public bool IsEndSeason(DbOffice dbOffice)
        {
            return dbOffice?.EndOfSeason == 1;
        }
        public void DeleteDatabaseFiles(bool reset)
        {
            var files = new[] { "Global.db", "Italian.db" };
            if (reset)
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }
        private DbRaceSummary FiltraPerNatzionalita(DbRaceSummary dbRaceSummary, Enums.TipoClassifica tipoClassifica)
        {
            if (tipoClassifica != Enums.TipoClassifica.it)
                return dbRaceSummary;

            return new DbRaceSummary
            {
                Season = dbRaceSummary.Season,
                Race = dbRaceSummary.Race,
                TrackName = dbRaceSummary.TrackName,
                Downloaded = dbRaceSummary.Downloaded,
                ApiRequestsRemaining = dbRaceSummary.ApiRequestsRemaining,
                Managers = dbRaceSummary.Managers
                    .Where(m => m.natCode == tipoClassifica.ToString())
                    .ToList()
            };
        }
        private DbRaceSummary OrdinaPerPosizioneArrivo(DbRaceSummary dbrace)
        {
            dbrace.Managers = dbrace.Managers
                .OrderBy(m => m.idx)
                .ToList();
            return dbrace;
        }
        private DbRaceSummary AssegnaPunti(DbRaceSummary dbRaceSummary, Enums.PointSystem pointSystem, Dictionary<int, int> puntiManagersStagione)
        {
            var points = GetPointSystem(pointSystem);

            for (int i = 0; i < dbRaceSummary.Managers.Count; i++)
            {
                dbRaceSummary.Managers[i].Points = i < points.Length ? points[i] : 0;

                int puntiStagione = puntiManagersStagione.TryGetValue(
                    dbRaceSummary.Managers[i].idm, out int value) ? value : 0;

                dbRaceSummary.Managers[i].TotPoints = puntiStagione + dbRaceSummary.Managers[i].Points;
            }

            return dbRaceSummary;
        }
        private Calendar FilterRaceEvents(Calendar calendar)
        {
            return new Calendar
            {
                Events = calendar.Events
                    .Where(ev => ev.EventType == "R")
                    .ToList()
            };
        }
        private int[] GetPointSystem(Enums.PointSystem pointSystem)
        {
            return pointSystem switch
            {
                Enums.PointSystem.F1 => new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 },
                Enums.PointSystem.Linear => new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                Enums.PointSystem.Gpro => new[] { 10, 8, 6, 5, 4, 3, 2, 1 },
                Enums.PointSystem.OldLinear => new[] { 40, 39, 38, 37, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(pointSystem))
            };
        }

    }
}

