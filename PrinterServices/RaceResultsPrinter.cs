using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Spectre.Console;
using System.Net;
using System.Text.RegularExpressions;
using Localizations;
using DataLayerServices;
using DbServices;
using System.Text;

namespace PrinterServices
{
    public class RaceResultsPrinter : IRaceResultPrinter
    {
        private const int PageSize = 25; // Numero di elementi per pagina
        private readonly ILocalizationManager _localizationManager;
        public RaceResultsPrinter(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }
        public void PrintResults(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica, bool isSeasonStandings = false)
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
                AddTableFooter(table, filteredManagers.Count, currentPage + 1, totalPages);

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
        private string BuildTitle(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica)
        {
            string tclassifica = _localizationManager.GetString(tipoClassifica switch
            {
                Enums.TipoClassifica.all => "ClassificaMondiale",
                Enums.TipoClassifica.it => "ClassificaItaliana",
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
                .AddColumn(new TableColumn(new Markup($"[bold]{Markup.Escape(_localizationManager.GetString("Nat"))}[/]")
        ).Centered()).AddColumn(new TableColumn($"[bold]{_localizationManager.GetString("PosizioneR")}[/]").Centered());

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
                string natCode = manager.natCode?.ToUpper() ?? "??";
                var nationalityCell = new Markup($"[bold]{Markup.Escape(natCode)}[/]", rowStyle);
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


                table.AddRow( positionCell, nameCell, nationalityCell, posCell, pointsCell, tyreCell, raceTimeCell, 
                    bestLapCell, avgSpeedCell, pitStopsCell, pitStopsBest);
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
            return decoded.Length > 20
                ? $"{decoded[..17]}..."
                : decoded;
        }
        private void AddTableFooter(Table table, int managerCount, int currentPage, int totalPages)
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
                choices.Add(_localizationManager.GetString("PreviousPage"));
            }

            if (currentPage < totalPages - 1)
            {
                choices.Add(_localizationManager.GetString("NextPage"));
                choices.Add(_localizationManager.GetString("LastPage"));
            }

            choices.Add(_localizationManager.GetString("SearchDriver"));
            choices.Add(_localizationManager.GetString("ExitPagination"));

            return choices;
        }
        private List<FoundDriver> FindDrivers(List<DbManager> managers, string searchTerm)
        {
            searchTerm = searchTerm.Trim();
            var results = new List<FoundDriver>();

            for (int i = 0; i < managers.Count; i++)
            {
                if (managers[i].Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
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
        public string GenerateBBCodeResults(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica, bool isSeasonStandings = false)
        {
            var filteredManagers = GetFilteredManagers(race.Managers, category, isSeasonStandings);
            var sb = new StringBuilder();

            // Intestazione
            sb.AppendLine($"[center][size=14][b]{BuildBBCodeTitle(race, category, tipoClassifica)}[/b][/size][/center]");
            sb.AppendLine();

            // Tabella
            sb.AppendLine("[table]");
            sb.AppendLine("[tr]");
            sb.AppendLine($"[th]{_localizationManager.GetString("Posizione")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("Manager")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("Nat")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("PosizioneR")}[/th]");
            sb.AppendLine($"[th]{(isSeasonStandings ? _localizationManager.GetString("PuntiTotali") : _localizationManager.GetString("Punti"))}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("Gomme")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("TempoGara")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("GiroVeloce")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("VelocitaMedia")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("PitStop")}[/th]");
            sb.AppendLine($"[th]{_localizationManager.GetString("BestPitStop")}[/th]");
            sb.AppendLine("[/tr]");

            int position = 1;
            foreach (var manager in filteredManagers)
            {
                sb.AppendLine("[tr]");

                // Formattazione posizione
                string positionCell = position switch
                {
                    1 => $"[b][color=gold]{_localizationManager.GetString("PrimoPosto")}[/color][/b]",
                    2 => $"[b][color=aqua]{_localizationManager.GetString("SecondoPosto")}[/color][/b]",
                    3 => $"[b][color=sandybrown]{_localizationManager.GetString("TerzoPosto")}[/color][/b]",
                    _ => $"{position}"
                };
                sb.AppendLine($"[td]{positionCell}[/td]");

                // Nome manager
                string name = FormatName(manager.Name);
                sb.AppendLine($"[td]{name}[/td]");
                sb.AppendLine($"[td][b]{manager.natCode?.ToUpper() ?? "??"}[/b][/td]");

                // Dati restanti
                sb.AppendLine($"[td]{manager.Pos}[/td]");
                sb.AppendLine($"[td]{(isSeasonStandings ? manager.TotPoints : manager.Points)}[/td]");
                sb.AppendLine($"[td]{manager.Tyre}[/td]");
                sb.AppendLine($"[td]{manager.RaceTime}[/td]");
                sb.AppendLine($"[td]{manager.BestLapTime}[/td]");
                sb.AppendLine($"[td]{manager.AvgSpeed}[/td]");
                sb.AppendLine($"[td]{manager.PitStops}[/td]");
                sb.AppendLine($"[td]{manager.BestPitTime}[/td]");

                sb.AppendLine("[/tr]");
                position++;
            }

            sb.AppendLine("[/table]");
            sb.AppendLine();

            // Footer
            sb.AppendLine($"[size=10][i]{_localizationManager.GetString("LastUpdate")}: {DateTime.Now.ToString("g", CultureInfo.CurrentCulture)} | ");
            sb.Append($"{filteredManagers.Count} {_localizationManager.GetString("Drivers")} | ");
            sb.Append($"{_localizationManager.GetString("Source")}: GPRO[/i][/size]");

            return sb.ToString();
        }
        private string BuildBBCodeTitle(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica)
        {
            string tclassifica = _localizationManager.GetString(tipoClassifica switch
            {
                Enums.TipoClassifica.all => "ClassificaMondiale",
                Enums.TipoClassifica.it => "ClassificaItaliana",
                _ => "NessunaClassifica"
            });

            return $"{_localizationManager.GetString("ClassificaStagionale")} {tclassifica.ToUpper()} " +
                   $"{_localizationManager.GetString("Categoria")} {category.ToUpper()} - " +
                   $"{_localizationManager.GetString("Stagione")} {race.Season} - " +
                   $"{_localizationManager.GetString("Gara")} {race.Race} - " +
                   $"{_localizationManager.GetString("Circuito")} ({race.TrackName})";
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

