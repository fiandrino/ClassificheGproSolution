using Spectre.Console;
using System.IO;
using System;
using Localizations;

namespace Mariottide
{
    public class Registrazione : IRegistrazione
    {
        private readonly ILocalizationManager _localizationManager;

        public Registrazione(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager;
        }

        public void InsertToken()
        {
            string filePath = GetTokenFilePath();
            string sel;

            do
            {
                AnsiConsole.Clear();
                ShowTokenInstructions(); // Mostra sempre le istruzioni

                bool tokenExists = File.Exists(filePath);
                string tokenOption = tokenExists
                    ? _localizationManager.GetString("EditTokenOption")
                    : _localizationManager.GetString("InsertTokenOption");

                sel = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title(_localizationManager.GetString("ChooseOption"))
                    .PageSize(10)
                    .AddChoices(
                        tokenOption,
                        _localizationManager.GetString("ExitOption"))
                    );

                if (sel == tokenOption)
                {
                    AnsiConsole.Clear();
                    ShowTokenInstructions(); // Mantieni visibili durante l'inserimento

                    string promptKey = tokenExists
                        ? "EditNewTokenPrompt"
                        : "InsertNewTokenPrompt";

                    string token = AnsiConsole.Prompt(
                        new TextPrompt<string>(_localizationManager.GetString(promptKey))
                            .Secret('-')
                    );

                    File.WriteAllText(filePath, token);
                    AnsiConsole.MarkupLine(
                        $"[green]{_localizationManager.GetString("TokenSavedSuccess")}[/]"
                    );
                    AnsiConsole.MarkupLine(_localizationManager.GetString("PressAnyKey"));
                    Console.ReadKey();
                }
            } while (sel != _localizationManager.GetString("ExitOption"));
        }

        public string ReadToken()
        {
            string filePath = GetTokenFilePath();
            if (!File.Exists(filePath))
            {
                InsertToken();
            }
            return File.ReadAllText(filePath);
        }

        private string GetTokenFilePath()
        {
            string appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appDataDir, "Mariottide");
            Directory.CreateDirectory(appDir);
            return Path.Combine(appDir, "password.txt");
        }

        private void ShowTokenInstructions()
        {
            var panel = new Panel(new Markup(
                $"[bold yellow]{_localizationManager.GetString("TokenInstructionsTitle")}[/]\n\n" +
                $"1. {_localizationManager.GetString("TokenStep1")}\n" +
                $"2. {_localizationManager.GetString("TokenStep2")}\n" +
                $"3. {_localizationManager.GetString("TokenStep3")}\n" +
                $"4. {_localizationManager.GetString("TokenStep4")}\n\n" +
                $"[italic]{_localizationManager.GetString("TokenSecurityWarning")}[/]"))
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader("[bold]GPRO[/]"),
                Padding = new Padding(1, 1, 1, 1)
            };

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }
    }
}