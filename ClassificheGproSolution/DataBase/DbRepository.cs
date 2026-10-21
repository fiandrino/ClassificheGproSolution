using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClassificheGproSolution
{
    public class DbRepository : IDbRepository
    {
        // Legge il calendario dal database
        public async Task<DbCalendar> ReadCalendar(TipoClassifica tipoClassifica)
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
        public async Task<DbRaceSummary> ReadLastRace(string category, TipoClassifica tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);

            // Verifica se il database esiste
            bool exists = context.Database.GetService<IRelationalDatabaseCreator>().Exists();

            if (exists)
            {
                // Recupera l'ultima gara per stagione e numero gara
                var races = await context.DbRaceSummaries
                    .Include(c => c.Managers)
                    .ToListAsync();
                DbRaceSummary dbRaceSummary = races
                    .OrderBy(r => r.Season)
                    .ThenBy(r => int.TryParse(r.Race, out int raceNumber) ? raceNumber : int.MinValue)
                    .ThenBy(r => r.Id)
                    .LastOrDefault();

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
        public async Task<DbOffice> ReadOffice(TipoClassifica tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            return await context.DbOffice.FirstOrDefaultAsync();
        }
        public async Task ClearOfficeData(TipoClassifica tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            var allOffices = await context.DbOffice.ToListAsync();
            context.DbOffice.RemoveRange(allOffices);
            await context.SaveChangesAsync();
        }
        //Salva Office
        public async Task SaveOffice(DbOffice dbOffice, TipoClassifica tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            var offices = await context.DbOffice.ToListAsync();
            var existingOffice = offices.FirstOrDefault();
            if (existingOffice == null)
            {
                context.DbOffice.Add(dbOffice);
            }
            else
            {
                existingOffice.SeasonNb = dbOffice.SeasonNb;
                existingOffice.RaceNb = dbOffice.RaceNb;
                existingOffice.TrackId = dbOffice.TrackId;
                existingOffice.EndOfSeason = dbOffice.EndOfSeason;
                existingOffice.NextSeasonResetDate = dbOffice.NextSeasonResetDate;
                existingOffice.UpdateInProgress = dbOffice.UpdateInProgress;
                existingOffice.Progress = dbOffice.Progress;
                existingOffice.ApiRequestsRemaining = dbOffice.ApiRequestsRemaining;

                context.DbOffice.RemoveRange(offices.Skip(1));
            }

            await context.SaveChangesAsync();
        }
        // Salva i risultati di una gara nel database
        public async Task SaveRaceSummary(DbRaceSummary dbrace, List<DbManager> orderedManagers, TipoClassifica tipoClassifica)
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
                int addedManagers = 0;
                foreach (var manager in orderedManagers)
                {
                    var existingManager = existingRace.Managers.FirstOrDefault(m =>
                        m.idm == manager.idm && m.Category == manager.Category);

                    if (existingManager == null)
                    {
                        existingRace.Managers.Add(manager);
                        addedManagers++;
                    }
                    else
                    {
                        CopyManagerValues(existingManager, manager);
                    }
                }

                existingRace.TrackName = dbrace.TrackName;
                existingRace.Downloaded = dbrace.Downloaded;
                existingRace.ApiRequestsRemaining = dbrace.ApiRequestsRemaining;

                Console.WriteLine($"Aggiornata gara esistente: Stagione {dbrace.Season}, Gara {dbrace.Race}");
                Console.WriteLine($"Aggiunti {addedManagers} nuovi manager");
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

        private static void CopyManagerValues(DbManager target, DbManager source)
        {
            target.idx = source.idx;
            target.natCode = source.natCode;
            target.Name = source.Name;
            target.Pos = source.Pos;
            target.Progress = source.Progress;
            target.Tyre = source.Tyre;
            target.RaceTime = source.RaceTime;
            target.LapsCompleted = source.LapsCompleted;
            target.PitStops = source.PitStops;
            target.BestLapTime = source.BestLapTime;
            target.AvgSpeed = source.AvgSpeed;
            target.BestPitTime = source.BestPitTime;
            target.Points = source.Points;
            target.TotPoints = source.TotPoints;
        }

        // Salva il calendario nel database
        public async Task SaveCalendar(DbCalendar dbCalendar, TipoClassifica tipoClassifica)
        {
            using var context = DbContextFactory.CreateContext(tipoClassifica);
            await context.Database.EnsureCreatedAsync();
            context.DbCalendars.Add(dbCalendar);
            await context.SaveChangesAsync();
        }
        // Nuovo metodo per recuperare i punti stagionali
        public async Task<Dictionary<int, int>> SommaPuntiGare(int season, string category, TipoClassifica tipoClassifica)
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
