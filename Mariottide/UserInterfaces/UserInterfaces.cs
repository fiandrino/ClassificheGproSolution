using System;
using System.Text;
using System.Threading.Tasks;
using DataLayerServices;
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
    }
}
