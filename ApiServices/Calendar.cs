using System.Collections.Generic;
using Newtonsoft.Json;

namespace ApiServices
{
    public class Calendar
    {
        public Calendar()
        {
            Events = new List<Events>();
        }
        public List<Events> Events { get; set; }

    }
    public class Events
    {
        [JsonProperty("eventType")]
        public string EventType { get; set; }
        [JsonProperty("idx")]
        public int Idx { get; set; }
        [JsonProperty("dateEvent")]
        public string DateEvent { get; set; }
    }
}
