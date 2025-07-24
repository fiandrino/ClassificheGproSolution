using System;
using System.Threading.Tasks;

namespace Mariottide
{
    public interface IUserInterfaces
    {
        void SelectLanguage();
        void ExitApplication(string message);
        TipoClassifica SelectTipoClassifica();
        MainMenuOption MainMenu();
        void ClearScreen();
        void Exception(Exception ex);
        Category SelectCategory();
        void InitializeConsole();
        Task ExitMessage();
    }
}
