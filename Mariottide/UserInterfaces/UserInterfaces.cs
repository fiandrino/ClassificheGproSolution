using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private ILocalizationManager _localizationManager; // Gestore della localizzazione

        // Costruttore per inizializzare il gestore della localizzazione
        public UserInterfaces(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        // Metodo per selezionare la lingua dell'interfaccia utente all'avvio del programma
        public void SelectLanguage()
        {
            // Mostra un'intestazione grafica
            AnsiConsole.Write(new Rule("[bold blue]Selezione lingua / Language selection[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            // Crea un prompt per la selezione della lingua
            SelectionPrompt<string> selectionPrompt = new();
            selectionPrompt.Title("[yellow]Seleziona la tua lingua / Select your language:[/]");
            selectionPrompt.PageSize(6);
            selectionPrompt.AddChoices("Italiano", "English", "Français", "Español", "Deutsch", "Русский");

            // Acquisisce la lingua selezionata dall'utente
            var prompt = AnsiConsole.Prompt(selectionPrompt);

            // Converte la lingua selezionata in un codice di cultura
            string cultureCode = prompt switch
            {
                "English" => "en",
                "Français" => "fr",
                "Español" => "es",
                "Deutsch" => "de",
                "Русский" => "ru",
                _ => "it" // Predefinito: italiano
            };

            // Imposta la cultura selezionata per l'applicazione
            _localizationManager.SetCulture(cultureCode);
        }

        // Metodo deprecato per la selezione della classifica; sostituito da GetRankingTypeFromUser()
        [Obsolete("Usa GetRankingTypeFromUser invece.")]
        public Enums.TipoClassifica SelectTipoClassifica()
        {
            // Ottiene le opzioni localizzate dal file di risorse
            var choices = new[]
            {
                _localizationManager.GetString("TipoClassificaOpzione1"),
                _localizationManager.GetString("TipoClassificaOpzione2"),
                _localizationManager.GetString("TipoClassificaOpzione3")
            };

            // Crea il prompt per la selezione
            SelectionPrompt<string> selectionPrompt = new();
            selectionPrompt.Title($"[bold yellow]{_localizationManager.GetString("TipoClassificaTitolo")}[/]");
            selectionPrompt.PageSize(3);
            selectionPrompt.AddChoices(choices);

            // Mostra il prompt e raccoglie la scelta
            var choice = AnsiConsole.Prompt(selectionPrompt);

            // Ritorna l'enum corrispondente in base alla scelta effettuata
            return choice == choices[0] ? Enums.TipoClassifica.it : Enums.TipoClassifica.all;
        }

        // Metodo aggiornato per selezionare il tipo di classifica usando una mappa localizzata
        public Enums.TipoClassifica GetRankingTypeFromUser()
        {
            // Mappatura manuale: enum -> chiave nel file di risorse
            var resourceKeys = new Dictionary<Enums.TipoClassifica, string>
            {
                { Enums.TipoClassifica.it, "TipoClassificaOpzione1" },
                { Enums.TipoClassifica.fr, "TipoClassificaOpzione2" },
                { Enums.TipoClassifica.all, "TipoClassificaOpzione3" },
                { Enums.TipoClassifica.ar, "TipoClassificaOpzione4" }
            };

            // Crea un dizionario: testo localizzato (escapato) -> valore enum
            var mapping = resourceKeys.ToDictionary(
                kvp => Markup.Escape(_localizationManager.GetString(kvp.Value)),
                kvp => kvp.Key);

            // Crea e mostra il prompt all'utente
            var selectionPrompt = new SelectionPrompt<string>()
                .Title($"[bold yellow]{Markup.Escape(_localizationManager.GetString("TipoClassificaTitolo"))}[/]")
                .PageSize(mapping.Count)
                .AddChoices(mapping.Keys);

            // Raccoglie la scelta e restituisce il valore enum
            var scelta = AnsiConsole.Prompt(selectionPrompt);
            return mapping[scelta];
        }

        // Mostra il menu principale all'utente e restituisce la scelta effettuata
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

            // Ritorna il valore enum corrispondente alla scelta effettuata
            return choice == choices[0] ? Enums.MainMenuOption.ViewStandings : Enums.MainMenuOption.Exit;
        }

        // Mostra il prompt per selezionare una categoria di gara
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

            // Restituisce l'enum corrispondente in base alla scelta effettuata
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

        // Mostra un messaggio di uscita e termina l'applicazione
        public void ExitApplication(string message)
        {
            AnsiConsole.MarkupLine($"[red]{message}[/]");
            Console.ReadKey();
            Environment.Exit(0);
        }

        // Pulisce completamente lo schermo della console
        public void ClearScreen()
        {
            AnsiConsole.Clear();
        }

        // Mostra in modo dettagliato un'eccezione e attende input utente
        public void Exception(Exception ex)
        {
            AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
            AnsiConsole.MarkupLine("[red]Premi un tasto per continuare...[/]");
            Console.ReadKey();
        }

        // Inizializza la console: titolo, encoding, directory di lavoro e pulizia schermo
        public void InitializeConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "Mariottide";
            CreateFolder("106"); // Crea la directory di lavoro "106"
            ClearScreen();
        }

        // Mostra il messaggio finale all'uscita con stile Figlet centrato
        public async Task ExitMessage()
        {
            ClearScreen();
            string message = _localizationManager.GetString("GoodbyeMessage");

            int height = Console.WindowHeight;
            int figletHeight = 6;
            int top = Math.Max(0, (height - figletHeight) / 2);
            Console.SetCursorPosition(0, top);

            AnsiConsole.Write(
                new FigletText(message)
                    .Centered()
                    .Color(Color.Green)
            );

            await Task.Delay(3000);
        }

        // Crea una cartella nella directory corrente, se non esiste già
        public void CreateFolder(string folderName)
        {
            try
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                string folderPath = Path.Combine(currentDirectory, folderName);

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

        // Mostra un pannello informativo durante il reset stagionale del gioco
        public void ResetMessage()
        {
            var panel = new Panel(
                new Markup(
                    "[bold yellow]ATTENZIONE:[/] Gpro è attualmente in fase di [underline]reset stagionale[/]!\n\n" +
                    "• Verrà archiviata la vecchia stagione\n" +
                    "• Verrà creata una nuova stagione\n" +
                    "• L'operazione richiede fino a 24 ore\n\n" +
                    "[grey]Aggiornamenti in tempo reale:[/] [link]https://www.gpro.net[/]"))
            {
                Border = BoxBorder.Rounded,
                Padding = new Padding(2, 1, 2, 1),
                Header = new PanelHeader("[bold red]⚙️ Reset Stagionale in Corso[/]")
            };

            panel.BorderColor(Color.Yellow);
            panel.Header.Centered();
            AnsiConsole.Write(panel);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[grey]Il reset stagionale richiede fino a 24 ore per il completamento.[/]");
            AnsiConsole.WriteLine();

            var scelta = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Seleziona un'opzione:[/]")
                    .PageSize(3)
                    .AddChoices("Torna al menu principale", "Rimani in attesa (simulazione)", "Esci dall'applicazione"));

            // Azioni in base alla scelta dell'utente
            switch (scelta)
            {
                case "Torna al menu principale":
                    return;

                case "Rimani in attesa (simulazione)":
                    AnsiConsole.Status()
                        .AutoRefresh(true)
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("yellow bold"))
                        .Start("Attesa completamento reset...", ctx =>
                        {
                            while (true)
                            {
                                System.Threading.Thread.Sleep(100); // Simulazione attesa infinita
                            }
                        });
                    break;

                case "Esci dall'applicazione":
                    Environment.Exit(0);
                    break;
            }
        }

        // Salva il BBCode della gara e della stagione in due file separati
        public void SaveBBCodeToFiles(string raceBBCode, string seasonBBCode, DbRaceSummary race, Enums.Category category, Enums.TipoClassifica tipoClassifica)
        {
            try
            {
                string baseFileName = $"S{race.Season}_R{race.Race}_{category}_{tipoClassifica}";
                string raceFileName = $"{baseFileName}_RaceStandings.txt";
                string seasonFileName = $"{baseFileName}_SeasonStandings.txt";

                File.WriteAllText(raceFileName, raceBBCode);
                File.WriteAllText(seasonFileName, seasonBBCode);

                AnsiConsole.MarkupLine("[green]File creati con successo:[/]");
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
