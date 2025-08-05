using System;
using System.Linq;
using System.Threading.Tasks;
using DataLayerServices;
using DbServices;
using PrinterServices;

namespace Mariottide
{
    /// <summary>
    /// Classe che gestisce la logica principale dell'applicazione,
    /// orchestrando l'interazione tra interfaccia utente, API, database e stampa risultati.
    /// </summary>
    public class BusinessLogic : IBusinessLogic
    {
        private readonly IRegistrazione _reg;
        private readonly IRaceResultPrinter _ptr;
        private readonly IUserInterfaces _ui;
        private readonly IDataLayer _dl;
        private string _token;

        /// <summary>
        /// Costruttore che inizializza le dipendenze necessarie alla logica di business.
        /// </summary>
        /// <param name="dl">Servizio per accesso ai dati</param>
        /// <param name="reg">Servizio per la registrazione e lettura token</param>
        /// <param name="ui">Servizio per la gestione dell'interfaccia utente</param>
        /// <param name="ptr">Servizio per la stampa dei risultati</param>
        public BusinessLogic(IDataLayer dl, IRegistrazione reg, IUserInterfaces ui, IRaceResultPrinter ptr)
        {
            _reg = reg;
            _ptr = ptr;
            _ui = ui;
            _dl = dl;
        }

        /// <summary>
        /// Avvia il ciclo principale dell'applicazione, mostrando il menu, elaborando i dati 
        /// e gestendo le scelte dell’utente.
        /// </summary>
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
                            await ProcessCategoryData(tipoClassifica, category).ConfigureAwait(false);
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

            await _ui.ExitMessage().ConfigureAwait(false);
        }

        /// <summary>
        /// Recupera ed elabora i dati per una specifica categoria e tipo classifica.
        /// Verifica se la stagione è terminata, in caso affermativo elimina i file locali.
        /// </summary>
        /// <param name="tipoClassifica">Tipo di classifica (es. it, all, ecc.)</param>
        /// <param name="category">Categoria (es. Rookie, Amateur, Master, Elite)</param>
        private async Task ProcessCategoryData(Enums.TipoClassifica tipoClassifica, Enums.Category category)
        {
            var dbOffice = await _dl.InitializeOffice(tipoClassifica.ToString(), _token).ConfigureAwait(false);
            bool reset = _dl.IsEndSeason(dbOffice);
            if (reset)
            {
                _dl.DeleteDatabaseFiles(reset);
                _ui.ResetMessage();
                return;
            }

            var dbCalendar = await _dl.InitializeCalendar(tipoClassifica.ToString(), _token).ConfigureAwait(false);
            if (dbCalendar?.Events == null)
                return;

            var (proxRaceDate, prevRaceDate) = _dl.GetRaceDates(dbCalendar.Events);
            if (proxRaceDate == new DateTime(0001, 01, 01)|| prevRaceDate == new DateTime(0001, 01, 01))
            {
                return;
            }

            var race = await _dl.GetRaceData(category.ToString(), tipoClassifica, proxRaceDate, prevRaceDate, _token).ConfigureAwait(false);
            if (race is null)
                return;

            PrintResults(tipoClassifica, category, race);
        }

        /// <summary>
        /// Stampa i risultati della gara e della stagione corrente nella categoria e classifica selezionata.
        /// </summary>
        /// <param name="tipoClassifica">Tipo di classifica</param>
        /// <param name="category">Categoria</param>
        /// <param name="race">Oggetto contenente i dati della gara</param>
        private void PrintResults(Enums.TipoClassifica tipoClassifica, Enums.Category category, DbRaceSummary race)
        {
            if (race?.Managers == null || !race.Managers.Any())
                return;
            _ptr.PrintResults(race, category.ToString(), tipoClassifica);
            _ptr.PrintResults(race, category.ToString(), tipoClassifica, isSeasonStandings: true);
            var raceBBCODE = _ptr.GenerateBBCodeResults(race, category.ToString(), tipoClassifica);
            var seasonBBCODE = _ptr.GenerateBBCodeResults(race, category.ToString(), tipoClassifica, true);
            // Salva i BBCode su file
            _ui.SaveBBCodeToFiles(raceBBCODE, seasonBBCODE, race, category, tipoClassifica);
        }
    }
}
