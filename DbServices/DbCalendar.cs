using System.Collections.Generic;

namespace DbServices
{
    public class DbCalendar
    {
        public DbCalendar()
        {
            Events = new List<DbEvent>();
        }

        public int Id { get; set; }
        public List<DbEvent> Events { get; set; }
    }
}
