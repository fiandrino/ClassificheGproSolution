using System;
using System.Linq;
using System.Threading.Tasks;
using DataLayerServices;
using DbServices;
using PrinterServices;

namespace Mariottide
{
    public class BusinessLogic : IBusinessLogic
    {
        private readonly IRegistrazione _reg;
        private readonly IRaceResultPrinter _ptr;
        private readonly IUserInterfaces _ui;
        private readonly IDataLayer _dl;

        private string _token;
        public BusinessLogic( IDataLayer dl, IRegistrazione reg, IUserInterfaces ui, IRaceResultPrinter ptr)
        {
            _reg = reg; _ptr = ptr; _ui = ui; _dl = dl;
        }
        public async Task ProcessData()
        {
            _ui.InitializeConsole();
            _ui.SelectLanguage();

            bool running = true;
            while (running)
            {
                _ui.ClearScreen();
                try
                {
                    var option = _ui.MainMenu();
                    switch (option)
                    {
                        case Enums.MainMenuOption.ViewStandings:
                            _token = _reg.ReadToken();
                            var tipoClassifica = _ui.SelectTipoClassifica();
                            var category = _ui.SelectCategory();
                            await ProcessCategoryData(tipoClassifica, category);
                            break;

                        case Enums.MainMenuOption.Exit:
                            running = false;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _ui.Exception(ex);
                }
            }

            await _ui.ExitMessage();
        }
        private async Task ProcessCategoryData(Enums.TipoClassifica tipoClassifica, Enums.Category category)
        {
            var dbOffice = await _dl.InitializeOffice(tipoClassifica.ToString(), _token);
            var reset = _dl.IsEndSeason(dbOffice);

            if (reset) 
            {
                _dl.DeleteDatabaseFiles(reset); 
            }
            else
            {
                var dbCalendar = await _dl.InitializeCalendar(tipoClassifica.ToString(), _token);
                var (proxRaceDate, prevRaceDate) = _dl.GetRaceDates(dbCalendar.Events);

                var race = await _dl.GetRaceData(category.ToString(), tipoClassifica, proxRaceDate, prevRaceDate, _token); ;
                PrintResults(tipoClassifica, category, race, race.ApiRequestsRemaining);
            }

        }
        private void PrintResults(Enums.TipoClassifica tipoClassifica, Enums.Category category, DbRaceSummary race, int richiesteRimanentiApi)
        {
            if (race == null || !race.Managers.Any())
                return;

            _ptr.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi);
            _ptr.PrintResults(race, category.ToString(), tipoClassifica, richiesteRimanentiApi, true);
        }
    }
}
