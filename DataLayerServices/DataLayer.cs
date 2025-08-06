using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApiServices;
using DbServices;

namespace DataLayerServices
{
    public class DataLayer : IDataLayer
    {
        private readonly IApiRequestGpro _api; // Servizio per effettuare richieste API verso GPRO
        private readonly IDbRepository _db;    // Servizio per interfacciarsi con il database locale

        // Costruttore che accetta le dipendenze necessarie per le chiamate API e l'accesso al database
        public DataLayer(IApiRequestGpro apiRequestGro, IDbRepository dbRepository)
        {
            this._api = apiRequestGro;
            this._db = dbRepository;
        }

        // Inizializza o aggiorna i dati dell'ufficio (Office) all'inizio o alla fine della stagione
        // Se la gara corrente è null o la 17, si presume sia l'inizio/fine stagione e si scaricano nuovi dati via API
        public async Task<DbOffice> InitializeOffice(string tipoClassifica, string token)
        {
            var dbOffice = await _db.ReadOffice(tipoClassifica.ToString()).ConfigureAwait(false);

            // Verifica se la gara è la prima o l'ultima della stagione
            if (dbOffice == null || Convert.ToInt32(dbOffice.RaceNb) == 17)
            {
                // Autenticazione all'API GPRO con il token fornito
                _api.Autenticazione(token);

                // Richiesta dei dati Office via API
                var office = await _api.GetApiAsync<Office>(UrlBase.UrlBaseOffice).ConfigureAwait(false);

                // Mapping dei dati Office da DTO a modello DB
                dbOffice = DtoMapper.MapToDbOffice(office);

                // Pulizia dei dati Office esistenti nel DB per il tipo di classifica
                await _db.ClearOfficeData(tipoClassifica.ToString()).ConfigureAwait(false);

                // Salvataggio dei nuovi dati Office nel database
                await _db.SaveOffice(dbOffice, tipoClassifica.ToString());
            }

            return dbOffice;
        }

        // Inizializza il calendario (Calendar) per la stagione
        // Se non esiste, effettua il download dai servizi API e salva i dati nel database
        public async Task<DbCalendar> InitializeCalendar(string tipoClassifica, string token)
        {
            var dbCalendar = await _db.ReadCalendar(tipoClassifica.ToString()).ConfigureAwait(false);

            // Se il calendario è già presente nel database, lo restituisce
            if (dbCalendar != null)
            {
                return dbCalendar;
            }
            else
            {
                // Autenticazione all'API GPRO con il token fornito
                _api.Autenticazione(token);

                // Scarica il calendario completo da API
                var calendar = await _api.GetApiAsync<Calendar>(UrlBase.UrlBaseCalendar).ConfigureAwait(false);

                // Filtra solo eventi di tipo gara
                calendar = FilterRaceEvents(calendar);

                // Mappa i dati nel formato compatibile con il DB
                dbCalendar = DtoMapper.MapToDbCalendar(calendar);

                // Salva i dati del calendario nel database
                await _db.SaveCalendar(dbCalendar, tipoClassifica.ToString()).ConfigureAwait(false);
            }

            return dbCalendar;
        }

        // Estrae la data della prossima e della precedente gara dalla lista eventi del calendario
        public (DateTime? proxRaceDate, DateTime? prevRaceDate) GetRaceDates(List<DbEvent> events)
        {
            // Prima data futura (>= oggi)
            var prox = events?
                .Where(d => d.DateEvent >= DateTime.Today)
                .OrderBy(d => d.DateEvent)
                .Select(d => d.DateEvent)
                .FirstOrDefault();

            // Ultima data passata (<= oggi)
            var prev = events?
                .Where(d => d.DateEvent <= DateTime.Today)
                .OrderByDescending(d => d.DateEvent)
                .Select(d => d.DateEvent)
                .FirstOrDefault();

            return (prox, prev);
        }

        // Ottiene i dati della gara dal database o li scarica se non aggiornati
        public async Task<DbRaceSummary> GetRaceData(string category, Enums.TipoClassifica tipoClassifica, DateTime? proxRaceDate, DateTime? prevRaceDate, string token)
        {
            // Recupera l'ultima gara salvata nel DB per categoria e tipo classifica
            var race = await _db.ReadLastRace(category.ToString(), tipoClassifica.ToString());

            // Se i dati sono obsoleti o mancanti, scarica una nuova gara e aggiorna il DB
            if (ShouldFetchNewRace(race, proxRaceDate, prevRaceDate))
            {
                race = await FetchAndProcessNewRace(category, tipoClassifica, proxRaceDate, token);
                await _db.SaveRaceSummary(race, race.Managers, tipoClassifica.ToString());
            }
            else
            {
                // In caso contrario, riordina i dati secondo la classifica attuale
                FiltraPerNatzionalita(race, tipoClassifica);
                OrdinaPerClassifica(race);
            }

            return race;
        }

        // Stabilisce se è necessario scaricare una nuova gara
        private bool ShouldFetchNewRace(DbRaceSummary race, DateTime? proxRaceDate, DateTime? prevRaceDate)
        {
            return (race == null || !race.Managers.Any()) || ((proxRaceDate > DateTime.Today && prevRaceDate < DateTime.Today && race.Downloaded < prevRaceDate));
        }

        // Scarica, filtra, ordina e assegna punti alla gara corrente
        private async Task<DbRaceSummary> FetchAndProcessNewRace(string category, Enums.TipoClassifica tipoClassifica, DateTime? proxRaceDate, string token)
        {
            _api.Autenticazione(token);
            var apiRace = await _api.GetApiAsync<RaceSummary>(UrlBase.urlBaseRaceSummary, category.ToString());

            // Mappa la risposta API in un oggetto compatibile con il DB
            var race = DtoMapper.MapToDbRace(apiRace, category.ToString());

            // Filtra manager per nazionalità se necessario (es. classifica IT)
            race = FiltraPerNatzionalita(race, tipoClassifica);

            #region Da considerare per essere spostato in una classe specifica che descrive uno specifico modo per classificare

                // TODO: Spostare in eventuale classe dedicata
                // Ordina i manager secondo la posizione di arrivo in gara
                race = OrdinaPerPosizioneArrivo(race);

                //TODO: Spostare in eventuale classe dedicata
                // Recupera i punti stagionali dei manager dal DB
                var puntiManagersStagione = await _db.SommaPuntiGare(
                        race.Season,
                        category.ToString(),
                        tipoClassifica.ToString());

                //TODO: Spostare in eventuale classe dedicata
                // Assegna i punti della gara attuale e aggiorna i punti totali
                race = AssegnaPunti(race, Enums.PointSystem.Gpro, puntiManagersStagione);

                //TODO: Spostare in eventuale classe dedicata
                // Ordina i manager per classifica
                race = OrdinaPerClassifica(race);

            #endregion

            return race;
        }

        // Ritorna true se la stagione è terminata
        public bool IsEndSeason(DbOffice dbOffice)
        {
            return dbOffice?.EndOfSeason == 1;
        }

        // Elimina i file del database locale se il flag reset è true
        public void DeleteDatabaseFiles(bool reset)
        {
            var files = new[] { "Global.db", "Italian.db" };
            if (reset)
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                        File.Delete(file);
                }
            }
        }

        // Filtra i manager per nazionalità italiana (solo se la classifica è italiana)
        private DbRaceSummary FiltraPerNatzionalita(DbRaceSummary dbRaceSummary, Enums.TipoClassifica tipoClassifica)
        {
            if (tipoClassifica == Enums.TipoClassifica.all)
                return dbRaceSummary;

            return new DbRaceSummary
            {
                Season = dbRaceSummary.Season,
                Race = dbRaceSummary.Race,
                TrackName = dbRaceSummary.TrackName,
                Downloaded = dbRaceSummary.Downloaded,
                ApiRequestsRemaining = dbRaceSummary.ApiRequestsRemaining,
                Managers = dbRaceSummary.Managers
                    .Where(m => m.natCode == tipoClassifica.ToString())
                    .ToList()
            };
        }

        // Ordina i manager secondo l'ordine di arrivo (idx crescente)
        private DbRaceSummary OrdinaPerPosizioneArrivo(DbRaceSummary dbrace)
        {
            dbrace.Managers = dbrace.Managers
                .OrderBy(m => m.idx)
                .ToList();
            return dbrace;
        }

        // Ordina i manager per classifica:
        // prima quelli con punti in ordine decrescente,
        // poi quelli senza punti in ordine crescente di idx
        private DbRaceSummary OrdinaPerClassifica(DbRaceSummary dbrace)
        {
            var conPunti = dbrace.Managers
                .Where(m => m.TotPoints > 0)
                .OrderByDescending(m => m.TotPoints);

            var senzaPunti = dbrace.Managers
                .Where(m => m.TotPoints == 0)
                .OrderBy(m => m.idx);

            dbrace.Managers = conPunti.Concat(senzaPunti).ToList();
            return dbrace;
        }

        // Assegna i punti gara ai manager e aggiorna i punti totali stagionali
        private DbRaceSummary AssegnaPunti(DbRaceSummary dbRaceSummary, Enums.PointSystem pointSystem, Dictionary<int, int> puntiManagersStagione)
        {
            var points = GetPointSystem(pointSystem);

            for (int i = 0; i < dbRaceSummary.Managers.Count; i++)
            {
                // Punti ottenuti nella gara corrente
                dbRaceSummary.Managers[i].Points = i < points.Length ? points[i] : 0;

                // Punti cumulativi stagione precedente
                int puntiStagione = puntiManagersStagione.TryGetValue(
                    dbRaceSummary.Managers[i].idm, out int value) ? value : 0;

                // Totale punti aggiornato
                dbRaceSummary.Managers[i].TotPoints = puntiStagione + dbRaceSummary.Managers[i].Points;
            }

            return dbRaceSummary;
        }

        // Filtra solo eventi di tipo "R" (gara) dal calendario
        private Calendar FilterRaceEvents(Calendar calendar)
        {
            return new Calendar
            {
                Events = calendar.Events
                    .Where(ev => ev.EventType == "R")
                    .ToList()
            };
        }

        // Restituisce la configurazione di punteggio in base al tipo specificato
        private int[] GetPointSystem(Enums.PointSystem pointSystem)
        {
            return pointSystem switch
            {
                Enums.PointSystem.F1 => new[] { 25, 18, 15, 12, 10, 8, 6, 4, 2, 1 },
                Enums.PointSystem.Linear => new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                Enums.PointSystem.Gpro => new[] { 10, 8, 6, 5, 4, 3, 2, 1 },
                Enums.PointSystem.OldLinear => new[] { 40, 39, 38, 37, 35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16, 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(pointSystem))
            };
        }
    }
}
