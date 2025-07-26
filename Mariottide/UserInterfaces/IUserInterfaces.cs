using System;
using System.Threading.Tasks;
using DataLayerServices;

namespace Mariottide
{
    public interface IUserInterfaces
    {
        void ClearScreen();
        void Exception(Exception ex);
        void ExitApplication(string message);
        Task ExitMessage();
        void InitializeConsole();
        Enums.MainMenuOption MainMenu();
        Enums.Category SelectCategory();
        void SelectLanguage();
        Enums.TipoClassifica SelectTipoClassifica();
        void CreateFolder(string folderName);
        void ResetMessage();
    }
}