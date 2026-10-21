using System.Collections.Generic;

public class DbCalendar
{
    public DbCalendar()
    {
        Events = new List<DbEvent>();
    }

    public int Id { get; set; }
    public List<DbEvent> Events { get; set; }
}
