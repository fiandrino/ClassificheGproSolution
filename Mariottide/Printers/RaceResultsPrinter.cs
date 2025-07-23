using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Spectre.Console;
using System.Net;
using System.Text.RegularExpressions;
using DbServices;
using Localizations;

namespace Mariottide
{
    public class RaceResultsPrinter : IRaceResultsPrinter
    {
        private const int PageSize = 25; // Numero di elementi per pagina
        private readonly ILocalizationManager _localizationManager;
        public RaceResultsPrinter(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }
        public void PrintResults(DbRaceSummary race, string category, TipoClassifica tipoClassifica, int requestRem, bool isSeasonStandings = false)
        {
            int currentPage = 0;
            var filteredManagers = GetFilteredManagers(race.Managers, category, isSeasonStandings);
            int totalPages = (int)Math.Ceiling(filteredManagers.Count / (double)PageSize);
            List<int> highlightedDriverIds = new List<int>(); // Memorizza gli ID dei piloti da evidenziare

            while (true)
            {
                AnsiConsole.Clear();
                AnsiConsole.WriteLine();

                string title = BuildTitle(race, category, tipoClassifica);
                var titlePanel = CreateTitlePanel(title);
                AnsiConsole.Write(titlePanel);
                AnsiConsole.WriteLine();

                var table = CreateTable(isSeasonStandings);
                var pageManagers = GetPageManagers(filteredManagers, currentPage);

                PopulateTable(table, pageManagers, isSeasonStandings, currentPage, highlightedDriverIds);
                AddTableFooter(table, filteredManagers.Count, currentPage + 1, totalPages, requestRem);

                AnsiConsole.Write(table);

                var choices = BuildPaginationChoices(currentPage, totalPages);

                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title(_localizationManager.GetString("PageNavigation"))
                        .AddChoices(choices)
                );

                if (selection == _localizationManager.GetString("NextPage") && currentPage < totalPages - 1)
                {
                    currentPage++;
                }
                else if (selection == _localizationManager.GetString("PreviousPage") && currentPage > 0)
                {
                    currentPage--;
                }
                else if (selection == _localizationManager.GetString("FirstPage") && currentPage > 0)
                {
                    currentPage = 0;
                }
                else if (selection == _localizationManager.GetString("LastPage") && currentPage < totalPages - 1)
                {
                    currentPage = totalPages - 1;
                }
                else if (selection == _localizationManager.GetString("SearchDriver"))
                {
                    string driverName = AnsiConsole.Ask<string>($"[yellow]{_localizationManager.GetString("EnterDriverNamePrompt")}:[/] ");
                    var foundDrivers = FindDrivers(filteredManagers, driverName);

                    if (foundDrivers.Any())
                    {
                        if (foundDrivers.Count == 1)
                        {
                            // Un solo pilota trovato: vai direttamente alla sua pagina
                            currentPage = foundDrivers.First().Page;
                            highlightedDriverIds = new List<int> { foundDrivers.First().Id };
                        }
                        else
                        {
                            // Più piloti trovati: mostra un menu di selezione
                            var selectedDriver = ShowDriverSelectionMenu(foundDrivers);
                            if (selectedDriver != null)
                            {
                                currentPage = selectedDriver.Page;
                                highlightedDriverIds = new List<int> { selectedDriver.Id };
                            }
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]{_localizationManager.GetString("DriverNotFound")}[/]");
                        Console.ReadKey();
                        highlightedDriverIds.Clear();
                    }
                }
                else if (selection == _localizationManager.GetString("ExitPagination"))
                {
                    break;
                }
            }
        }
        private string BuildTitle(DbRaceSummary race, string category, TipoClassifica tipoClassifica)
        {
            string tclassifica = _localizationManager.GetString(tipoClassifica switch
            {
                TipoClassifica.all => "ClassificaMondiale",
                TipoClassifica.it => "ClassificaItaliana",
                _ => "NessunaClassifica"
            });

            return $"{_localizationManager.GetString("ClassificaStagionale")} [bold]{tclassifica.ToUpper()}[/] " +
                   $"{_localizationManager.GetString("Categoria")} [bold]{category.ToUpper()}[/] - " +
                   $"{_localizationManager.GetString("Stagione")} [bold]{race.Season}[/] - " +
                   $"{_localizationManager.GetString("Gara")} [bold]{race.Race}[/] - " +
                   $"{_localizationManager.GetString("Circuito")} [bold]({race.TrackName})[/]";
        }
        private Panel CreateTitlePanel(string title)
        {
            return new Panel(new Markup(title))
            {
                Border = BoxBorder.None,
                Padding = new Padding(1, 0, 1, 0),
                //Header = new PanelHeader("[bold yellow]🏁 CLASSIFICA UFFICIALE 🏁[/]", Justify.Center)
            };
        }
        private Table CreateTable(bool isSeasonStandings)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("Posizione")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("Manager")}[/]").LeftAligned())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("PosizioneR")}[/]").Centered());

            if (isSeasonStandings)
            {
                table.AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("PuntiTotali")}[/]").Centered());
            }
            else
            {
                table.AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("Punti")}[/]").Centered());
            }

            table.AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("Gomme")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("TempoGara")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("GiroVeloce")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("VelocitaMedia")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("PitStop")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("BestPitStop")}[/]").Centered());

            return table;
        }
        private List<DbManager> GetFilteredManagers(List<DbManager> managers, string category, bool isSeasonStandings)
        {
            return managers
                .Where(c => c.Category == category)
                .OrderByDescending(m => isSeasonStandings ? m.TotPoints : m.Points)
                .ToList();
        }
        private void PopulateTable(Table table, List<DbManager> managers, bool isSeasonStandings, int currentPage, List<int> highlightedDriverIds)
        {
            int startPosition = currentPage * PageSize;

            for (int i = 0; i < managers.Count; i++)
            {
                int absolutePosition = startPosition + i + 1;
                var manager = managers[i];

                Style rowStyle = GetRowStyle(absolutePosition);
                string positionMarkup = GetPositionString(absolutePosition);
                string name = FormatName(manager.Name);

                // Controlla se questo pilota deve essere evidenziato
                bool isHighlighted = highlightedDriverIds.Contains(manager.idm);

                // Applica l'evidenziazione se necessario
                string nameMarkup = isHighlighted ?
                    $"[bold red]{name}[/]" :
                    $"[{(absolutePosition <= 3 ? "bold" : "")}]{name}[/]";

                var positionCell = new Markup(positionMarkup);
                var nameCell = new Markup(nameMarkup, rowStyle);
                var posCell = new Text(manager.Pos, rowStyle);

                var pointsCell = isSeasonStandings
                    ? new Text(manager.TotPoints.ToString(), rowStyle)
                    : new Text(manager.Points.ToString(), rowStyle);

                var tyreCell = new Text(manager.Tyre, rowStyle);
                var raceTimeCell = new Text(manager.RaceTime, rowStyle);
                var bestLapCell = new Text(manager.BestLapTime, rowStyle);
                var avgSpeedCell = new Text(manager.AvgSpeed, rowStyle);
                var pitStopsCell = new Text(manager.PitStops, rowStyle);
                var pitStopsBest = new Text(manager.BestPitTime, rowStyle);

                table.AddRow(
                    positionCell,
                    nameCell,
                    posCell,
                    pointsCell,
                    tyreCell,
                    raceTimeCell,
                    bestLapCell,
                    avgSpeedCell,
                    pitStopsCell,
                    pitStopsBest
                );
            }
        }
        private Style GetRowStyle(int position)
        {
            return position switch
            {
                1 => new Style(Color.Gold1),
                2 => new Style(Color.Aqua),
                3 => new Style(Color.SandyBrown),
                _ => Style.Plain
            };
        }
        private string GetPositionString(int position)
        {
            return position switch
            {
                1 => $"[gold1 bold]{_localizationManager.GetString("PrimoPosto")}[/]",
                2 => $"[aqua bold]{_localizationManager.GetString("SecondoPosto")}[/]",
                3 => $"[sandybrown bold]{_localizationManager.GetString("TerzoPosto")}[/]",
                _ => $"{position}"
            };
        }
        private string FormatName(string name)
        {
            string decoded = DecodeUnicodeEntities(name);
            // Non facciamo più l'escape del markup qui
            return decoded.Length > 20
                ? decoded.Substring(0, 17) + "..."
                : decoded;
        }
        private void AddTableFooter(Table table, int managerCount, int currentPage, int totalPages, int requestRem)
        {
            table.Caption = new TableTitle(
                $"[grey]{_localizationManager.GetString("LastUpdate")}: {DateTime.Now.ToString("g", CultureInfo.CurrentCulture)} | " +
                $"{managerCount} {_localizationManager.GetString("Drivers")} | " +
                $"{_localizationManager.GetString("Page")} {currentPage} {_localizationManager.GetString("Of")} {totalPages} | " +
                $"{_localizationManager.GetString("Source")}: GPRO[/]");
        }
        public string DecodeUnicodeEntities(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Decodifica HTML standard
            string decoded = WebUtility.HtmlDecode(input);

            // Decodifica ulteriore per sequenze numeriche Unicode
            return Regex.Replace(
                decoded,
                @"\&\#(\d+);",
                m => char.ConvertFromUtf32(int.Parse(m.Groups[1].Value))
            );
        }
        private List<DbManager> GetPageManagers(List<DbManager> managers, int page)
        {
            return managers
                .Skip(page * PageSize)
                .Take(PageSize)
                .ToList();
        }
        private List<string> BuildPaginationChoices(int currentPage, int totalPages)
        {
            var choices = new List<string>();

            if (currentPage > 0)
            {
                choices.Add(_localizationManager.GetString("FirstPage"));
            }

            if (currentPage > 0)
            {
                choices.Add(_localizationManager.GetString("PreviousPage"));
            }

            if (currentPage < totalPages - 1)
            {
                choices.Add(_localizationManager.GetString("NextPage"));
            }

            if (currentPage < totalPages - 1)
            {
                choices.Add(_localizationManager.GetString("LastPage"));
            }

            choices.Add(_localizationManager.GetString("SearchDriver"));
            choices.Add(_localizationManager.GetString("ExitPagination"));

            return choices;
        }
        private List<FoundDriver> FindDrivers(List<DbManager> managers, string searchTerm)
        {
            searchTerm = searchTerm.Trim().ToLower();
            var results = new List<FoundDriver>();

            for (int i = 0; i < managers.Count; i++)
            {
                if (managers[i].Name.ToLower().Contains(searchTerm))
                {
                    results.Add(new FoundDriver
                    {
                        Id = managers[i].idm,
                        Name = managers[i].Name,
                        Page = i / PageSize,
                        Position = i + 1
                    });
                }
            }
            return results;
        }
        private FoundDriver ShowDriverSelectionMenu(List<FoundDriver> drivers)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(new Rule($"[yellow]{_localizationManager.GetString("MultipleDriversFound")}[/]").RuleStyle("grey").Centered());

            var choices = drivers.Select(d =>
                $"{d.Name} - {_localizationManager.GetString("Position")} {d.Position}"
            ).ToList();

            choices.Add(_localizationManager.GetString("CancelSelection"));

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(_localizationManager.GetString("SelectDriverPrompt"))
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (selection == _localizationManager.GetString("CancelSelection"))
                return null;

            int selectedIndex = choices.IndexOf(selection);
            return drivers[selectedIndex];
        }
        private class FoundDriver
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Page { get; set; }
            public int Position { get; set; }
        }
    }

}
