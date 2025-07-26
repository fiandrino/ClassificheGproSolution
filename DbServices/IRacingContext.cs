using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DbServices
{
    public interface IRacingContext : IDisposable
    {
        DbSet<DbManager> DbManagers { get; set; }
        DbSet<DbRaceSummary> DbRaceSummaries { get; set; }
        DbSet<DbCalendar> DbCalendars { get; set; }
        DbSet<DbEvent> DbEvents { get; set; }
        DbSet<DbOffice> DbOffice { get; set; }

        DatabaseFacade Database { get; }
        int SaveChanges();
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        EntityEntry Entry(object entity);
    }

}