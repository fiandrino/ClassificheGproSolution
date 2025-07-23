using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiServices;
using DbServices;
using Localizations;
using Spectre.Console;
using Calendar = ApiServices.Calendar;

namespace Mariottide
{
    public enum Category
    {
        Rookie, Amateur, Pro, Master, Elite
    }
    public enum PointSystem
    {
        F1, Linear,Gpro,
        Quaranta
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
        private readonly IApiRequestGpro _apiRequestGpro;
        private readonly ILocalizationManager _localizationManager;
        private readonly IDbRepository _dbRepository;
        private readonly IRegistrazione _registrazione;
        private readonly IRaceResultsPrinter _raceResultPrinter;
        private string _token;
        public BusinessLogic(
            IApiRequestGpro apiRequestGpro,
            ILocalizationManager localizationManager,
            IDbRepository dbRepository,
            IRegistrazione registrazione,
            IRaceResultsPrinter raceResultsPrinter)
        {
            _apiRequestGpro = apiRequestGpro;
            _localizationManager = localizationManager;
            _dbRepository = dbRepository;
            _registrazione = registrazione;
            _raceResultPrinter = raceResultsPrinter;
        }
        public async Task ProcessData()
        {
            InitializeConsole();
            SelectLanguage();
            _token = _registrazione.ReadToken();

            while (true)
            {
                AnsiConsole.Clear();
                try
                {
                    var choice = ShowMainMenu();
                    if (choice == MainMenuOption.Exit)
                    {
                        break;
                    }

                    if (choice == MainMenuOption.ViewStandings)
                    {
                        var tipoClassifica = SelectTipoClassifica();
                        var category = SelectCategory();
                        await ProcessCategoryData(tipoClassifica, category);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
                    AnsiConsole.MarkupLine("[red]Premi un tasto per continuare...[/]");
                    Console.ReadKey();
                }
            }
            await ExitMessage();
        }
        private async Task ExitMessage()
        {
            AnsiConsole.Clear();

            // Ottieni il messaggio
            string message = _localizationManager.GetString("GoodbyeMessage");

            // Calcola il centro verticale
            int height = Console.WindowHeight;
            int figletHeight = 6; // Altezza approssimativa del font Figlet
            int top = Math.Max(0, (height - figletHeight) / 2);

            // Posiziona il cursore
            Console.SetCursorPosition(0, top);

            // Mostra testo grande centrato
            AnsiConsole.Write(
                new FigletText(message)
                    .Centered()
                    .Color(Color.Green)
            );

            await Task.Delay(3000);
        }
        private void InitializeConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Mariottide";
            AnsiConsole.Clear();
        }
        private void SelectLanguage()
        {
            AnsiConsole.Write(new Rule("[bold blue]Selezione lingua / Language selection[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Seleziona la tua lingua / Select your language:[/]")
                    .PageSize(6)
                    .AddChoices(
                        "Italiano",
                        "English",
                        "Français",
                        "Español",
                        "Deutsch",
                        "Русский"
                    ));

            _localizationManager.SetCulture(choice switch
            {
                "English" => "en",
                "Français" => "fr",
                "Español" => "es",
                "Deutsch" => "de",
                "Русский" => "ru",
                _ => "it"
            });
        }
        private TipoClassifica SelectTipoClassifica()
        {
            var choices = new[]
            {
                _localizationManager.GetString("TipoClassificaOpzione1"),
                _localizationManager.GetString("TipoClassificaOpzione2")
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold yellow]{_localizationManager.GetString("TipoClassificaTitolo")}[/]")
                    .PageSize(3)
                    .AddChoices(choices));

            return choice == choices[0] ? TipoClassifica.it : TipoClassifica.all;
        }
        private Category SelectCategory()
        {
            var choices = new[]
            {
                _localizationManager.GetString("ClassificaOpzione1"),
                _localizationManager.GetString("ClassificaOpzione2"),
                _localizationManager.GetString("ClassificaOpzione3"),
                _localizationManager.GetString("ClassificaOpzione4"),
                _localizationManager.GetString("ClassificaOpzione5")
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold yellow]{_localizationManager.GetString("ClassificaTitolo")}[/]")
                    .PageSize(5)
                    .AddChoices(choices));

            return choice switch
            {
                string s when s == choices[0] => Category.Rookie,
                string s when s == choices[1] => Category.Amateur,
                string s when s == choices[2] => Category.Pro,
                string s when s == choices[3] => Category.Master,
                string s when s == choices[4] => Category.Elite,
                _ => Category.Pro
            };
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
            var dbOffice = await _dbRepository.ReadOffice(tipoClassifica.ToString());
            if (dbOffice != null)
            {
                return dbOffice;
            }
            _apiRequestGpro.Autenticazione(_token);
            var office = await _apiRequestGpro.GetApiAsync<Office>(UrlBase.UrlBaseOffice);
            dbOffice = MapToDbOffice(office);
            await _dbRepository.SaveOffice(dbOffice, tipoClassifica.ToString());

            return dbOffice;
        }
        private async Task<DbCalendar> InitializeCalendar(TipoClassifica tipoClassifica)
        {
            var dbCalendar = await _dbRepository.ReadCalendar(tipoClassifica.ToString());
            if (dbCalendar != null) return dbCalendar;
            _apiRequestGpro.Autenticazione(_token);
            var calendar = await _apiRequestGpro.GetApiAsync<Calendar>(UrlBase.UrlBaseCalendar);
            calendar = FilterRaceEvents(calendar);
            dbCalendar = MapToDbCalendar(calendar);
            await _dbRepository.SaveCalendar(dbCalendar, tipoClassifica.ToString());

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
        private async Task<DbRaceSummary> GetRaceData(Category category, TipoClassifica tipoClassifica, DateTime? proxRaceDate, DateTime? prevRaceDate )
        {
            var race = await _dbRepository.ReadLastRace(category.ToString(), tipoClassifica.ToString());

            if (ShouldFetchNewRace(race, proxRaceDate, prevRaceDate))
            {
                race = await FetchAndProcessNewRace(category, tipoClassifica, proxRaceDate);
                await _dbRepository.SaveRaceSummary(race, race.Managers, tipoClassifica.ToString());
            }

            return race;
        }
        private bool ShouldFetchNewRace(DbRaceSummary race, DateTime? proxRaceDate, DateTime? prevRaceDate )
        {
            return (race == null || !race.Managers.Any()) || ((proxRaceDate > DateTime.Today && prevRaceDate < DateTime.Today && race.Downloaded < prevRaceDate));
        }
        private async Task<DbRaceSummary> FetchAndProcessNewRace(Category category, TipoClassifica tipoClassifica, DateTime? proxRaceDate)
        {
            _apiRequestGpro.Autenticazione(_token);
            var apiRace = await _apiRequestGpro.GetApiAsync<RaceSummary>( UrlBase.urlBaseRaceSummary, category.ToString());

            var race = MapToDbRace(apiRace, category.ToString());
            race = FiltraPerNatzionalita(race, tipoClassifica);
            race = OrdinaPerPosizioneArrivo(race);

            var puntiManagersStagione = await _dbRepository.SommaPuntiGare(
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
            ExitApplication("Gpro è in reset. Il programma verrà chiuso alla pressione di un tasto");
        }
        private void DeleteDatabaseFiles()
        {
            var files = new[] { "Global", "Italian" };
            foreach (var file in files.Where(File.Exists))
            {
                File.Delete(file);
            }
        }
        private void ExitApplication(string message)
        {
            AnsiConsole.MarkupLine($"[red]{message}[/]");
            Console.ReadKey();
            Environment.Exit(0);
        }
        private void PrintResults(TipoClassifica tipoClassifica, Category category, DbRaceSummary race, int richiesteRimanentiApi)
        {
            if (race == null || !race.Managers.Any())
                return;

            _raceResultPrinter.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi);
            _raceResultPrinter.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi, true);
        }
        private DbRaceSummary MapToDbRace(RaceSummary apiModel, string category)
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
        private DbCalendar MapToDbCalendar(Calendar calendar)
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
        private DbOffice MapToDbOffice(Office apiOffice)
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
        private DateTime ConvertToDateTimeForOffice(string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
                throw new ArgumentException("La stringa della data non può essere vuota", nameof(dateString));

            string cleanedDate = Regex.Replace(dateString, @"(\d+)(st|nd|rd|th)\b", "$1");
            string fullDateString = $"{cleanedDate} {DateTime.Now.Year}";
            string format = "MMM dd yyyy";

            return DateTime.ParseExact(
                fullDateString,
                format,
                CultureInfo.InvariantCulture
            );
        }
        private DateTime ConvertToDateTime(string dateString)
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
                CultureInfo.InvariantCulture
            );
        }
        private MainMenuOption ShowMainMenu()
        {
            var choices = new[]
            {
        _localizationManager.GetString("MainMenuViewStandings"),
        _localizationManager.GetString("MainMenuExit")
    };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold yellow]{_localizationManager.GetString("MainMenuTitle")}[/]")
                    .PageSize(10)
                    .AddChoices(choices));

            return choice == choices[0] ? MainMenuOption.ViewStandings : MainMenuOption.Exit;
        }
    }
}
