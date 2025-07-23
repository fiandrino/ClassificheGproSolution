using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace DbServices
{
    public class DbEvent
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public int Idx { get; set; }
        public DateTime DateEvent { get; set; }

        [ForeignKey(nameof(Calendar))]
        public int CalendarId { get; set; }

        public DbCalendar Calendar { get; set; }
    }
}