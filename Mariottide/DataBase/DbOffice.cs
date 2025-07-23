using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Mariottide
{
    public class DbOffice
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string SeasonNb { get; set; }
        [Required]
        public string RaceNb { get; set; }
        [Required]
        public string TrackId { get; set; }
        [Required]
        public int EndOfSeason { get; set; }
        [Required]
        public DateTime NextSeasonResetDate { get; set; }
        [Required]
        public int UpdateInProgress { get; set; }
        [Required]
        public int Progress { get; set; }
        [Required]
        public int ApiRequestsRemaining { get; set; }
    }
}

