using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataLayerServices;
using DbServices;
using Localizations;
using Spectre.Console;
using Rule = Spectre.Console.Rule;

namespace Mariottide
{
    public class UserInterfaces : IUserInterfaces
    {
        private ILocalizationManager _localizationManager;

        public UserInterfaces(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }
        public void SelectLanguage()
        {
            AnsiConsole.Write(new Rule("[bold blue]Selezione lingua / Language selection[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            SelectionPrompt<string> selectionPrompt = new();
            selectionPrompt.Title("[yellow]Seleziona la tua lingua / Select your language:[/]");
            selectionPrompt.PageSize(6);
            selectionPrompt.AddChoices("Italiano", "English", "Français", "Español", "Deutsch", "Русский");
            var prompt = AnsiConsole.Prompt(selectionPrompt);
            string cultureCode = prompt switch
            {
                "English" => "en",
                "Français" => "fr",
                "Español" => "es",
                "Deutsch" => "de",
                "Русский" => "ru",
                _ => "it"
            };
            _localizationManager.SetCulture(cultureCode);
        }
        public Enums.TipoClassifica SelectTipoClassifica()
        {
            var choices = new[]
            {
                _localizationManager.GetString("TipoClassificaOpzione1"),
                _localizationManager.GetString("TipoClassificaOpzione2")
            };

            SelectionPrompt<string> selectionPrompt = new();

            selectionPrompt.Title($"[bold yellow]{_localizationManager.GetString("TipoClassificaTitolo")}[/]");
            selectionPrompt.PageSize(3);
            selectionPrompt.AddChoices(choices);
            var choice = AnsiConsole.Prompt(selectionPrompt);
            return choice == choices[0] ? Enums.TipoClassifica.it : Enums.TipoClassifica.all;
        }
        public Enums.MainMenuOption MainMenu()
        {
            var choices = new[]
            {
                _localizationManager.GetString("MainMenuViewStandings"),
                _localizationManager.GetString("MainMenuExit")
            };
            SelectionPrompt<string> selectionPrompt = new();

            selectionPrompt.Title($"[bold yellow]{_localizationManager.GetString("MainMenuTitle")}[/]");
            selectionPrompt.PageSize(10);
            selectionPrompt.AddChoices(choices);

            var choice = AnsiConsole.Prompt(selectionPrompt);
            return choice == choices[0] ? Enums.MainMenuOption.ViewStandings : Enums.MainMenuOption.Exit;
        }
        public Enums.Category SelectCategory()
        {
            var choices = new[]
           {
                _localizationManager.GetString("ClassificaOpzione1"),
                _localizationManager.GetString("ClassificaOpzione2"),
                _localizationManager.GetString("ClassificaOpzione3"),
                _localizationManager.GetString("ClassificaOpzione4"),
                _localizationManager.GetString("ClassificaOpzione5")
            };

            SelectionPrompt<string> selectionPrompt = new();

            selectionPrompt.Title($"[bold yellow]{_localizationManager.GetString("ClassificaTitolo")}[/]");
            selectionPrompt.PageSize(5);
            selectionPrompt.AddChoices(choices);

            var choice = AnsiConsole.Prompt(selectionPrompt);
            return choice switch
            {
                string s when s == choices[0] => Enums.Category.Rookie,
                string s when s == choices[1] => Enums.Category.Amateur,
                string s when s == choices[2] => Enums.Category.Pro,
                string s when s == choices[3] => Enums.Category.Master,
                string s when s == choices[4] => Enums.Category.Elite,
                _ => Enums.Category.Pro
            };
        }
        public void ExitApplication(string message)
        {
            AnsiConsole.MarkupLine($"[red]{message}[/]");
            Console.ReadKey();
            Environment.Exit(0);
        }
        public void ClearScreen()
        {
            AnsiConsole.Clear();
        }
        public void Exception(Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            AnsiConsole.MarkupLine("[red]Premi un tasto per continuare...[/]");
            Console.ReadKey();
        }
        public void InitializeConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Mariottide";
            CreateFolder("106");
            ClearScreen();
        }
        public async Task ExitMessage()
        {
            ClearScreen();

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
        public void CreateFolder(string folderName)
        {
            try
            {
                // Ottieni il percorso della directory corrente
                string currentDirectory = Directory.GetCurrentDirectory();

                // Combina il percorso con il nome della cartella
                string folderPath = Path.Combine(currentDirectory, folderName);

                // Crea la cartella se non esiste già
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore: {ex.Message}");
            }
        }
        public void ResetMessage()
        {
            // Crea un pannello con il messaggio di reset
            var panel = new Panel(
                new Markup(
                    "[bold yellow]ATTENZIONE:[/] Gpro è attualmente in fase di [underline]reset stagionale[/]!\n\n" +
                    "• Verrà archiviata la vecchia stagione\n" +
                    "• Verrà creata una nuova stagione\n" +
                    "• L'operazione richiede fino a 24 ore\n\n" +
                    "[grey]Aggiornamenti in tempo reale:[/] [link]https://www.gpro.net[/]")
            )
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1),
                Header = new PanelHeader("[bold red]⚙️ Reset Stagionale in Corso[/]"),
            };

            // Personalizza i colori del pannello
            panel.BorderColor(Color.Yellow);
            panel.Header.Centered();

            // Mostra il pannello
            AnsiConsole.Write(panel);

            // Mostra il messaggio informativo sul tempo
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Il reset stagionale richiede fino a 24 ore per il completamento.[/]");
            AnsiConsole.WriteLine();

            // Crea un menu di selezione
            var scelta = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Seleziona un'opzione:[/]")
                    .PageSize(3)
                    .AddChoices(new[] {
                "Torna al menu principale",
                "Rimani in attesa (simulazione)",
                "Esci dall'applicazione"
                    }));

            // Gestisci la selezione
            switch (scelta)
            {
                case "Torna al menu principale":
                    return;

                case "Rimani in attesa (simulazione)":
                    // Simulazione di attesa con spinner
                    AnsiConsole.Status()
                        .AutoRefresh(true)
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow bold"))
                        .Start("Attesa completamento reset...", ctx =>
                        {
                            while (true)
                            {
                        // Uscita con CTRL+C
                        System.Threading.Thread.Sleep(100);
                            }
                        });
                    break;

                case "Esci dall'applicazione":
                    Environment.Exit(0);
                    break;
            }
        }
        public void SaveBBCodeToFiles(string raceBBCode, string seasonBBCode, DbRaceSummary race, Enums.Category category, Enums.TipoClassifica tipoClassifica)
        {
            try
            {
                // Creazione del nome base del file
                string baseFileName = $"S{race.Season}_R{race.Race}_{category}_{tipoClassifica}";

                // Generazione dei nomi file completi
                string raceFileName = $"{baseFileName}_RaceStandings.txt";
                string seasonFileName = $"{baseFileName}_SeasonStandings.txt";

                // Salvataggio dei file
                File.WriteAllText(raceFileName, raceBBCode);
                File.WriteAllText(seasonFileName, seasonBBCode);

                // Messaggio di conferma
                AnsiConsole.MarkupLine($"[green]File creati con successo:[/]");
                AnsiConsole.MarkupLine($"[yellow]{raceFileName}[/]");
                AnsiConsole.MarkupLine($"[yellow]{seasonFileName}[/]");
                AnsiConsole.MarkupLine($"[grey]Directory: {Directory.GetCurrentDirectory()}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Errore durante il salvataggio:[/] {ex.Message}");
            }
        }
    }
}
