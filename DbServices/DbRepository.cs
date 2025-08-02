using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DbServices
{
    public class DbRepository : IDbRepository
    {
        /// <summary>
        /// Legge il calendario più recente dal database
        /// </summary>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        /// <returns>Calendario completo con eventi associati o null se non presente</returns>
        public async Task<DbCalendar> ReadCalendar(string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();

            // Recupera il calendario più recente
            return await context.DbCalendars
                .Include(c => c.Events)
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Legge l'ultima gara salvata per una specifica categoria
        /// </summary>
        /// <param name="category">Categoria da filtrare (es. "GT3")</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        /// <returns>Dettagli della gara con i manager filtrati per categoria o null se non presente</returns>
        public async Task<DbRaceSummary> ReadLastRace(string category, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);

            // Verifica se il database esiste
            bool exists = context.Database.GetService<IRelationalDatabaseCreator>().Exists();

            if (exists)
            {
                // Recupera l'ultima gara per stagione e numero gara
                DbRaceSummary dbRaceSummary = await context.DbRaceSummaries
                    .Include(c => c.Managers)
                    .OrderByDescending(s => s.Season)
                    .ThenByDescending(r => r.Race)
                    .FirstOrDefaultAsync();

                if (dbRaceSummary != null)
                {
                    // Filtra i manager per categoria
                    List<DbManager> managers = dbRaceSummary.Managers.FindAll(c => c.Category == category).ToList();
                    dbRaceSummary.Managers = managers;
                    return dbRaceSummary;
                }
                return null;
            }
            else
            {
                // Crea il database se non esiste
                await context.Database.EnsureCreatedAsync();
                return null;
            }
        }

        /// <summary>
        /// Legge i dati dell'ufficio (office) dal database
        /// </summary>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        /// <returns>Record DbOffice o null se non presente</returns>
        public async Task<DbOffice> ReadOffice(string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            return await context.DbOffice.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Cancella tutti i dati dell'ufficio dal database
        /// </summary>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        public async Task ClearOfficeData(string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            var allOffices = await context.DbOffice.ToListAsync();
            context.DbOffice.RemoveRange(allOffices);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Salva i dati dell'ufficio nel database se non esiste già un record con la stessa data di reset
        /// </summary>
        /// <param name="dbOffice">Dati dell'ufficio da salvare</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        public async Task SaveOffice(DbOffice dbOffice, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            var existingOffice = await context.DbOffice.FirstOrDefaultAsync(r => r.NextSeasonResetDate == dbOffice.NextSeasonResetDate);
            if (existingOffice == null)
            {
                context.DbOffice.Add(dbOffice);
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sostituisce completamente i dati dell'ufficio nel database
        /// </summary>
        /// <param name="dbOffice">Dati dell'ufficio da salvare</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        public async Task ClearAndSaveOffice(DbOffice dbOffice, string tipoClassifica)
        {
            if (dbOffice == null)
                return;

            using var context = DbContextFactory.CreateContext(tipoClassifica);

            // Controlla se esiste già un record nel database
            var existingOffice = await context.DbOffice.FirstOrDefaultAsync();

            if (existingOffice != null)
            {
                // Sovrascrivi i dati esistenti con i nuovi valori
                context.Entry(existingOffice).CurrentValues.SetValues(dbOffice);
            }
            else
            {
                // Se non esiste alcun record, aggiungine uno nuovo
                await context.DbOffice.AddAsync(dbOffice);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Salva i risultati di una gara nel database
        /// </summary>
        /// <param name="dbrace">Dati della gara da salvare</param>
        /// <param name="orderedManagers">Lista dei manager ordinati per la gara</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        public async Task SaveRaceSummary(DbRaceSummary dbrace, List<DbManager> orderedManagers, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            // Cerca una gara esistente con la stessa stagione e numero gara
            var existingRace = await context.DbRaceSummaries
                .Include(r => r.Managers)
                .FirstOrDefaultAsync(r =>
                    r.Season == dbrace.Season &&
                    r.Race == dbrace.Race);

            if (existingRace != null)
            {
                // Identifica i nuovi manager da aggiungere
                var newManagers = orderedManagers
                    .Where(nm => !existingRace.Managers
                        .Any(em => em.idm == nm.idm))
                    .ToList();

                // Aggiungi i nuovi manager alla gara esistente
                foreach (var newManager in newManagers)
                {
                    existingRace.Managers.Add(newManager);
                }
            }
            else
            {
                // NUOVA GARA: aggiungi tutto il record
                dbrace.Managers = orderedManagers;
                context.DbRaceSummaries.Add(dbrace);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Salva un calendario completo nel database
        /// </summary>
        /// <param name="dbCalendar">Dati del calendario da salvare</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        public async Task SaveCalendar(DbCalendar dbCalendar, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            context.DbCalendars.Add(dbCalendar);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Calcola la somma dei punti per tutti i manager in una specifica stagione e categoria
        /// </summary>
        /// <param name="season">Stagione di riferimento</param>
        /// <param name="category">Categoria da filtrare</param>
        /// <param name="tipoClassifica">Tipo di classifica per identificare il contesto del database</param>
        /// <returns>Dizionario con ID manager e punti totali accumulati</returns>
        public async Task<Dictionary<int, int>> SommaPuntiGare(int season, string category, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);

            return await context.DbManagers
                .Where(m => m.RaceSummary.Season == season && m.Category == category)
                .GroupBy(m => m.idm)
                .Select(g => new
                {
                    ManagerId = g.Key,
                    TotalPoints = g.Sum(m => m.Points)
                })
                .ToDictionaryAsync(x => x.ManagerId, x => x.TotalPoints);
        }
    }
}