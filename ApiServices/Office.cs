using Newtonsoft.Json;

namespace ApiServices
{
    public class Office
    {
        [JsonProperty("loadingDataState")]
        public int LoadingDataState { get; set; }

        [JsonProperty("seasonNb")]
        public string SeasonNb { get; set; }

        [JsonProperty("raceNb")]
        public string RaceNb { get; set; }

        [JsonProperty("trackId")]
        public string TrackId { get; set; }

        [JsonProperty("endOfSeason")]
        public int EndOfSeason { get; set; }

        [JsonProperty("nextSeasonResetDate")]
        public string NextSeasonResetDate { get; set; }

        [JsonProperty("updateInProgress")]
        public int UpdateInProgress { get; set; }

        [JsonProperty("progress")]
        public int Progress { get; set; }
        
        [JsonProperty("apiRequestsRemaining")]
        public int ApiRequestsRemaining { get; set; }
    }
}
