using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DbServices
{
    public class DbRepository : IDbRepository
    {
        // Legge il calendario dal database
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
        // Legge l'ultima gara salvata nel database per una categoria
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
                    .OrderBy(s => s.Season)
                    .OrderBy(r => r.Race)
                    .LastOrDefaultAsync();

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
        //Legge Office
        public async Task<DbOffice> ReadOffice(string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            return await context.DbOffice.FirstOrDefaultAsync();
        }
        public async Task ClearOfficeData(string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            var allOffices = await context.DbOffice.ToListAsync();
            context.DbOffice.RemoveRange(allOffices);
            await context.SaveChangesAsync();
        }
        //Salva Office
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
        // Salva i risultati di una gara nel database
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

                Console.WriteLine($"Aggiornata gara esistente: Stagione {dbrace.Season}, Gara {dbrace.Race}");
                Console.WriteLine($"Aggiunti {newManagers.Count} nuovi manager");
            }
            else
            {
                // NUOVA GARA: aggiungi tutto il record
                dbrace.Managers = orderedManagers;
                context.DbRaceSummaries.Add(dbrace);
                Console.WriteLine($"Aggiunta nuova gara: Stagione {dbrace.Season}, Gara {dbrace.Race}");
            }

            await context.SaveChangesAsync();
        }
        // Salva il calendario nel database
        public async Task SaveCalendar(DbCalendar dbCalendar, string tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            context.DbCalendars.Add(dbCalendar);
            await context.SaveChangesAsync();
        }
        // Nuovo metodo per recuperare i punti stagionali
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
        public bool IsEndSeason(DbOffice dbOffice)
        {
            if (dbOffice?.EndOfSeason == 1)
                return true;
            return false;
        }
        public void DeleteDatabaseFiles(bool reset)
        {
            var files = new[] { "Global", "Italian" };
            if (reset)
            {
                foreach (var file in files.Where(File.Exists))
                {
                    File.Delete(file);
                }
            }
         }

    }
}
