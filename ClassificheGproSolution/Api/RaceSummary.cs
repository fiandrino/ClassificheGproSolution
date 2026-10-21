using System.Collections.Generic;
using Newtonsoft.Json;

namespace ClassificheGproSolution
{
    public class RaceSummary
    {
        public RaceSummary()
        {
            Managers = new List<Managers>();
        }

        [JsonProperty("season")]
        public string Season { get; set; }

        [JsonProperty("race")]
        public string Race { get; set; }

        [JsonProperty("trackName")]
        public string TrackName { get; set; }

        [JsonProperty("apiRequestsRemaining")]
        public int ApiRequestsRemaining { get; set; }

        [JsonProperty("managers")]
        public List<Managers> Managers { get; set; } 
    }

    public class Managers
    {

        [JsonProperty("idx")]
        public string idx { get; set; }

        [JsonProperty("natCode")]
        public string natCode { get; set; }

        [JsonProperty("IDM")]
        public int idm { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("pos")]
        public string Pos { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }

        [JsonProperty("tyre")]
        public string Tyre { get; set; }

        [JsonProperty("raceTime")]
        public string RaceTime { get; set; }

        [JsonProperty("lapsCompleted")]
        public string LapsCompleted { get; set; }

        [JsonProperty("pitStops")]
        public string PitStops { get; set; }

        [JsonProperty("bestLapTime")]
        public string BestLapTime { get; set; }

        [JsonProperty("avgSpeed")]
        public string AvgSpeed { get; set; }

        [JsonProperty("bestPitTime")]
        public string BestPitTime { get; set; }
    }
}

