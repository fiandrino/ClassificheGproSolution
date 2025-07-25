using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiServices;
using DbServices;

namespace Mariottide
{
    public enum Category
    {
        Rookie, Amateur, Pro, Master, Elite
    }
    public enum PointSystem
    {
        F1, Linear, Gpro, Quaranta
    }
    public enum TipoClassifica
    {
        it, all
    }
    public enum MainMenuOption
    {
        ViewStandings, Exit
    }

    public class BusinessLogic : IBusinessLogic
    {
        private readonly IApiRequestGpro _api;
        private readonly IDbRepository _db;
        private readonly IRegistrazione _reg;
        private readonly IRaceResultsPrinter _printer;
        private readonly IUserInterfaces _ui;
        private string _token;
        public BusinessLogic(
            IApiRequestGpro apiRequestGpro,
            IDbRepository dbRepository,
            IRegistrazione registrazione,
            IUserInterfaces userInterfaces,
            IRaceResultsPrinter raceResultsPrinter)
        {
            _api = apiRequestGpro;
            _db = dbRepository;
            _reg = registrazione;
            _printer = raceResultsPrinter;
            _ui = userInterfaces;
        }
        public async Task ProcessData()
        {
            _ui.InitializeConsole();
            _ui.SelectLanguage();

            bool running = true;
            while (running)
            {
                _ui.ClearScreen();
                try
                {
                    var option = _ui.MainMenu();
                    switch (option)
                    {
                        case MainMenuOption.ViewStandings:
                            _token = _reg.ReadToken();
                            var tipoClassifica = _ui.SelectTipoClassifica();
                            var category = _ui.SelectCategory();
                            await ProcessCategoryData(tipoClassifica, category);
                            break;

                        case MainMenuOption.Exit:
                            running = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _ui.Exception(ex);
                }
            }

            await _ui.ExitMessage();
        }

        private async Task ProcessCategoryData(TipoClassifica tipoClassifica, Category category)
        {
            var dbOffice = await InitializeOffice(tipoClassifica);
            CheckForReset(dbOffice);

            var dbCalendar = await InitializeCalendar(tipoClassifica);
            var (proxRaceDate, prevRaceDate) = GetRaceDates(dbCalendar.Events);

            var race = await GetRaceData(category, tipoClassifica, proxRaceDate, prevRaceDate);
            PrintResults(tipoClassifica, category, race, race.ApiRequestsRemaining);
        }
        private async Task<DbOffice> InitializeOffice(TipoClassifica tipoClassifica)
        {
            var dbOffice = await _db.ReadOffice(tipoClassifica.ToString());
            if (dbOffice != null)
            {
                return dbOffice;
            }
            _api.Autenticazione(_token);
            var office = await _api.GetApiAsync<Office>(UrlBase.UrlBaseOffice);
            dbOffice = DtoMapper.MapToDbOffice(office);
            await _db.SaveOffice(dbOffice, tipoClassifica.ToString());

            return dbOffice;
        }
        private async Task<DbCalendar> InitializeCalendar(TipoClassifica tipoClassifica)
        {
            var dbCalendar = await _db.ReadCalendar(tipoClassifica.ToString());
            if (dbCalendar != null) return dbCalendar;
            _api.Autenticazione(_token);
            var calendar = await _api.GetApiAsync<Calendar>(UrlBase.UrlBaseCalendar);
            calendar = FilterRaceEvents(calendar);
            dbCalendar = DtoMapper.MapToDbCalendar(calendar);
            await _db.SaveCalendar(dbCalendar, tipoClassifica.ToString());

            return dbCalendar;
        }
        private (DateTime? proxRaceDate, DateTime? prevRaceDate) GetRaceDates(List<DbEvent> events)
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
        private async Task<DbRaceSummary> GetRaceData(Category category, TipoClassifica tipoClassifica, DateTime? proxRaceDate, DateTime? prevRaceDate)
        {
            var race = await _db.ReadLastRace(category.ToString(), tipoClassifica.ToString());

            if (ShouldFetchNewRace(race, proxRaceDate, prevRaceDate))
            {
                race = await FetchAndProcessNewRace(category, tipoClassifica, proxRaceDate);
                await _db.SaveRaceSummary(race, race.Managers, tipoClassifica.ToString());
            }

            return race;
        }
        private bool ShouldFetchNewRace(DbRaceSummary race, DateTime? proxRaceDate, DateTime? prevRaceDate)
        {
            return (race == null || !race.Managers.Any()) || ((proxRaceDate > DateTime.Today && prevRaceDate < DateTime.Today && race.Downloaded < prevRaceDate));
        }
        private async Task<DbRaceSummary> FetchAndProcessNewRace(Category category, TipoClassifica tipoClassifica, DateTime? proxRaceDate)
        {
            _api.Autenticazione(_token);
            var apiRace = await _api.GetApiAsync<RaceSummary>(UrlBase.urlBaseRaceSummary, category.ToString());

            var race = DtoMapper.MapToDbRace(apiRace, category.ToString());
            race = FiltraPerNatzionalita(race, tipoClassifica);
            race = OrdinaPerPosizioneArrivo(race);

            var puntiManagersStagione = await _db.SommaPuntiGare(
                race.Season,
                category.ToString(),
                tipoClassifica.ToString());

            return AssegnaPunti(race, PointSystem.F1, puntiManagersStagione);
        }
        private DbRaceSummary FiltraPerNatzionalita(DbRaceSummary dbRaceSummary, TipoClassifica tipoClassifica)
        {
            if (tipoClassifica != TipoClassifica.it)
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
        private DbRaceSummary AssegnaPunti(DbRaceSummary dbRaceSummary, PointSystem pointSystem, Dictionary<int, int> puntiManagersStagione)
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
        private int[] GetPointSystem(PointSystem pointSystem)
        {
            return pointSystem switch
            {
                PointSystem.F1 => new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 },
                PointSystem.Linear => new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                PointSystem.Gpro => new[] { 10, 8, 6, 5, 4, 3, 2, 1 },
                PointSystem.Quaranta => new[] { 40, 39, 38, 37, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(pointSystem))
            };
        }
        private void CheckForReset(DbOffice dbOffice)
        {
            if (dbOffice?.EndOfSeason != 1)
                return;

            DeleteDatabaseFiles();
            _ui.ExitApplication("Reset gpro");
        }
        private void DeleteDatabaseFiles()
        {
            var files = new[] { "Global", "Italian" };
            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }
        }
        private void PrintResults(TipoClassifica tipoClassifica, Category category, DbRaceSummary race, int richiesteRimanentiApi)
        {
            if (race == null || !race.Managers.Any())
                return;

            _printer.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi);
            _printer.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi, true);
        }


    }
}
