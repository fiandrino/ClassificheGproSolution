using Microsoft.EntityFrameworkCore;

namespace ClassificheGproSolution
{
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