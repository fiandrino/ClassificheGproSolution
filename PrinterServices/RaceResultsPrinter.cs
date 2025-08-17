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
        private const int PageSize = 35; // Numero di elementi per pagina
        private readonly ILocalizationManager _loc;
        public RaceResultsPrinter(ILocalizationManager localizationManager)
        {
            _loc = localizationManager;
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

                // Aggiunta della leggenda come panel separato
                var legendPanel = new Panel(new Markup(
                        $"[italic yellow]LEGENDA:[/] " +
                        $"[bold]C.P[/] = Category Position, " +
                        $"[bold]R.P[/] = Race Position, " +
                        $"[bold]N.P[/] = New Points, " +
                        $"[bold]T.P[/] = Total Points"))
                {
                    Border = BoxBorder.None,
                    Padding = new Padding(1, 0, 1, 0)
                };
                AnsiConsole.Write(legendPanel);

                var table = CreateTable(isSeasonStandings);
                var pageManagers = GetPageManagers(filteredManagers, currentPage);

                PopulateTable(table, pageManagers, isSeasonStandings, currentPage, highlightedDriverIds);
                AddTableFooter(table, filteredManagers.Count, currentPage + 1, totalPages);

                AnsiConsole.Write(table);

                var choices = BuildPaginationChoices(currentPage, totalPages);

                var selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title(_loc.GetString("PageNavigation"))
                        .AddChoices(choices));

                if (selection == _loc.GetString("NextPage") && currentPage < totalPages - 1)
                {
                    currentPage++;
                }
                else if (selection == _loc.GetString("PreviousPage") && currentPage > 0)
                {
                    currentPage--;
                }
                else if (selection == _loc.GetString("FirstPage") && currentPage > 0)
                {
                    currentPage = 0;
                }
                else if (selection == _loc.GetString("LastPage") && currentPage < totalPages - 1)
                {
                    currentPage = totalPages - 1;
                }
                else if (selection == _loc.GetString("SearchDriver"))
                {
                    string driverName = AnsiConsole.Ask<string>($"[yellow]{_loc.GetString("EnterDriverNamePrompt")}:[/] ");
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
                        AnsiConsole.MarkupLine($"[red]{_loc.GetString("DriverNotFound")}[/]");
                        Console.ReadKey();
                        highlightedDriverIds.Clear();
                    }
                }
                else if (selection == _loc.GetString("ExitPagination"))
                {
                    break;
                }
            }
        }
        private string BuildTitle(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica)
        {
            string tclassifica = _loc.GetString(tipoClassifica switch
            {
                Enums.TipoClassifica.all => "ClassificaMondiale",
                Enums.TipoClassifica.it => "ClassificaItaliana",
                Enums.TipoClassifica.fr => "ClassificaFrancese",
                Enums.TipoClassifica.ar => "ClassificaArgentina",
                _ => "NessunaClassifica"
            });

            return $"{_loc.GetString("ClassificaStagionale")} [bold]{tclassifica.ToUpper()}[/] " +
                   $"{_loc.GetString("Categoria")} [bold]{category.ToUpper()}[/] - " +
                   $"{_loc.GetString("Stagione")} [bold]{race.Season}[/] - " +
                   $"{_loc.GetString("Gara")} [bold]{race.Race}[/] - " +
                   $"{_loc.GetString("Circuito")} [bold]({race.TrackName})[/]";
        }
        private Panel CreateTitlePanel(string title)
        {
            return new Panel(new Markup(title))
            {
                Border = BoxBorder.None,
                Padding = new Padding(1, 0, 1, 0),
                //Header = new PanelHeader("[bold yellow]PADC = Posizione assoluta di classifica[/]", Justify.Center)
            };
        }
        private Table CreateTable(bool isSeasonStandings)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("Posizione")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("PosAssoluta")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("Manager")}[/]").LeftAligned())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("Nat")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("PosizioneR")}[/]").Centered());

            if (isSeasonStandings)
            {
                table.AddColumn(new TableColumn($"[bold]{_loc.GetString("PuntiGara")}[/]").Centered());
                table.AddColumn(new TableColumn($"[bold]{_loc.GetString("PuntiTotali")}[/]").Centered());
            }
            else
            {
                table.AddColumn(new TableColumn($"[bold]{_loc.GetString("PuntiGara")}[/]").Centered());
            }

            table.AddColumn(new TableColumn($"[bold]{_loc.GetString("Gomme")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("TempoGara")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("GiroVeloce")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("VelocitaMedia")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("PitStop")}[/]").Centered())
                .AddColumn(new TableColumn($"[bold]{_loc.GetString("BestPitStop")}[/]").Centered());

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
                int position = startPosition + i + 1;
                var manager = managers[i];

                Style rowStyle = GetRowStyle(position);
                string positionMarkup = GetPositionString(position);
                var wordAbsPositionPos = new Text(manager.idx.ToString(), rowStyle);
                string name = FormatName(manager.Name);

                bool isHighlighted = highlightedDriverIds.Contains(manager.idm);
                string nameMarkup = isHighlighted ?
                    $"[bold red]{name}[/]" :
                    $"[{(position <= 3 ? "bold" : "")}]{name}[/]";

                string natCode = manager.natCode?.ToUpper() ?? "??";
                var nationalityCell = new Markup($"[bold]{Markup.Escape(natCode)}[/]", rowStyle);
                var positionCell = new Markup(positionMarkup);
                var nameCell = new Markup(nameMarkup, rowStyle);
                var posCell = new Text(manager.Pos, rowStyle);

                // MODIFICA QUI: Crea sempre entrambe le celle
                var totalPointsCell = new Text(manager.TotPoints.ToString(), rowStyle);
                var racePointsCell = new Text(manager.Points.ToString(), rowStyle);

                var tyreCell = new Text(manager.Tyre, rowStyle);
                var raceTimeCell = new Text(manager.RaceTime, rowStyle);
                var bestLapCell = new Text(manager.BestLapTime, rowStyle);
                var avgSpeedCell = new Text(manager.AvgSpeed, rowStyle);
                var pitStopsCell = new Text(manager.PitStops, rowStyle);
                var pitStopsBest = new Text(manager.BestPitTime, rowStyle);

                // MODIFICA QUI: Aggiungi le celle in modo condizionale
                if (isSeasonStandings)
                {
                    table.AddRow(
                        positionCell,
                        wordAbsPositionPos,
                        nameCell,
                        nationalityCell,
                        posCell,
                        racePointsCell,    // Punti gara
                        totalPointsCell,   // Punti totali stagione
                        tyreCell,
                        raceTimeCell,
                        bestLapCell,
                        avgSpeedCell,
                        pitStopsCell,
                        pitStopsBest
                    );
                }
                else
                {
                    table.AddRow(
                        positionCell,
                        wordAbsPositionPos,
                        nameCell,
                        nationalityCell,
                        posCell,
                        racePointsCell,    // Solo punti gara
                        tyreCell,
                        raceTimeCell,
                        bestLapCell,
                        avgSpeedCell,
                        pitStopsCell,
                        pitStopsBest
                    );
                }
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
                1 => $"[gold1 bold]{_loc.GetString("PrimoPosto")}[/]",
                2 => $"[aqua bold]{_loc.GetString("SecondoPosto")}[/]",
                3 => $"[sandybrown bold]{_loc.GetString("TerzoPosto")}[/]",
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
                $"[grey]{_loc.GetString("LastUpdate")}: {DateTime.Now.ToString("g", CultureInfo.CurrentCulture)} | " +
                $"{managerCount} {_loc.GetString("Drivers")} | " +
                $"{_loc.GetString("Page")} {currentPage} {_loc.GetString("Of")} {totalPages} | " +
                $"{_loc.GetString("Source")}: GPRO[/]");
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
                choices.Add(_loc.GetString("FirstPage"));
                choices.Add(_loc.GetString("PreviousPage"));
            }

            if (currentPage < totalPages - 1)
            {
                choices.Add(_loc.GetString("NextPage"));
                choices.Add(_loc.GetString("LastPage"));
            }

            choices.Add(_loc.GetString("SearchDriver"));
            choices.Add(_loc.GetString("ExitPagination"));

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
            AnsiConsole.Write(new Rule($"[yellow]{_loc.GetString("MultipleDriversFound")}[/]").RuleStyle("grey").Centered());

            var choices = drivers.Select(d =>
                $"{d.Name} - {_loc.GetString("Position")} {d.Position}"
            ).ToList();

            choices.Add(_loc.GetString("CancelSelection"));

            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(_loc.GetString("SelectDriverPrompt"))
                    .PageSize(15)
                    .AddChoices(choices)
            );

            if (selection == _loc.GetString("CancelSelection"))
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
            sb.AppendLine($"[th]{_loc.GetString("Posizione")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("Manager")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("Nat")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("PosizioneR")}[/th]");

            // MODIFICA QUI: Gestione colonne punti
            if (isSeasonStandings)
            {
                sb.AppendLine($"[th]{_loc.GetString("PuntiTotali")}[/th]");
                sb.AppendLine($"[th]{_loc.GetString("PuntiGara")}[/th]");
            }
            else
            {
                sb.AppendLine($"[th]{_loc.GetString("Punti")}[/th]");
            }

            sb.AppendLine($"[th]{_loc.GetString("Gomme")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("TempoGara")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("GiroVeloce")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("VelocitaMedia")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("PitStop")}[/th]");
            sb.AppendLine($"[th]{_loc.GetString("BestPitStop")}[/th]");
            sb.AppendLine("[/tr]");

            int position = 1;
            foreach (var manager in filteredManagers)
            {
                sb.AppendLine("[tr]");

                // Formattazione posizione
                string positionCell = position switch
                {
                    1 => $"[b][color=gold]{_loc.GetString("PrimoPosto")}[/color][/b]",
                    2 => $"[b][color=aqua]{_loc.GetString("SecondoPosto")}[/color][/b]",
                    3 => $"[b][color=sandybrown]{_loc.GetString("TerzoPosto")}[/color][/b]",
                    _ => $"{position}"
                };
                sb.AppendLine($"[td]{positionCell}[/td]");

                // Nome manager
                string name = FormatName(manager.Name);
                sb.AppendLine($"[td]{name}[/td]");
                sb.AppendLine($"[td][b]{manager.natCode?.ToUpper() ?? "??"}[/b][/td]");
                sb.AppendLine($"[td]{manager.Pos}[/td]");

                // MODIFICA QUI: Gestione celle punti
                if (isSeasonStandings)
                {
                    sb.AppendLine($"[td]{manager.TotPoints}[/td]");
                    sb.AppendLine($"[td]{manager.Points}[/td]");
                }
                else
                {
                    sb.AppendLine($"[td]{manager.Points}[/td]");
                }

                // Dati restanti
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
            sb.AppendLine($"[size=10][i]{_loc.GetString("LastUpdate")}: {DateTime.Now.ToString("g", CultureInfo.CurrentCulture)} | ");
            sb.Append($"{filteredManagers.Count} {_loc.GetString("Drivers")} | ");
            sb.Append($"{_loc.GetString("Source")}: GPRO[/i][/size]");

            return sb.ToString();
        }
        private string BuildBBCodeTitle(DbRaceSummary race, string category, Enums.TipoClassifica tipoClassifica)
        {
            string tclassifica = _loc.GetString(tipoClassifica switch
            {
                Enums.TipoClassifica.all => "ClassificaMondiale",
                Enums.TipoClassifica.it => "ClassificaItaliana",
                _ => "NessunaClassifica"
            });

            return $"{_loc.GetString("ClassificaStagionale")} {tclassifica.ToUpper()} " +
                   $"{_loc.GetString("Categoria")} {category.ToUpper()} - " +
                   $"{_loc.GetString("Stagione")} {race.Season} - " +
                   $"{_loc.GetString("Gara")} {race.Race} - " +
                   $"{_loc.GetString("Circuito")} ({race.TrackName})";
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

