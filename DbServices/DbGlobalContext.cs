using Microsoft.EntityFrameworkCore;

namespace DbServices
{
    //TODO: considrare l'uso delle MIgrations
    //TODO: VAlutare il rifacimento delle tabelle del db per l'uso di un di un db unico per ogni stato
    public class DbGlobalContext : DbContext, IRacingContext
    {
        public DbSet<DbManager> DbManagers { get; set; }
        public DbSet<DbRaceSummary> DbRaceSummaries { get; set; }
        public DbSet<DbCalendar> DbCalendars { get; set; }
        public DbSet<DbEvent> DbEvents { get; set; }
        public DbSet<DbOffice> DbOffice { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseSqlite("Data Source=Global.db");
        }
    }

}