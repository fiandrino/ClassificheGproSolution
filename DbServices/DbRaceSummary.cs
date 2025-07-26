using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DbServices
{
    public class DbRaceSummary
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Season { get; set; } // <-- Rimosso [StringLength(50)]

        [Required]
        [StringLength(100)]
        public string Race { get; set; }

        [Required]
        [StringLength(100)]
        public string TrackName { get; set; }

        public DateTime? Downloaded { get; set; }

        [Required]
        public int ApiRequestsRemaining { get; set; }

        // Relazione uno-a-molti con DbManager
        public List<DbManager> Managers { get; set; } = new List<DbManager>();
    }

    public class DbManager
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int idx { get; set; } // <-- Rimosso [StringLength(20)]

        [Required]
        [StringLength(3)]
        public string natCode { get; set; }

        public int idm { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(10)]
        public string Pos { get; set; }

        public int Progress { get; set; }

        [Required]
        [StringLength(20)]
        public string Tyre { get; set; }

        [Required]
        [StringLength(20)]
        public string RaceTime { get; set; }

        [Required]
        [StringLength(10)]
        public string LapsCompleted { get; set; }

        [Required]
        [StringLength(10)]
        public string PitStops { get; set; }

        [Required]
        [StringLength(20)]
        public string BestLapTime { get; set; }

        [Required]
        [StringLength(20)]
        public string AvgSpeed { get; set; }

        [Required]
        [StringLength(20)]
        public string BestPitTime { get; set; }

        [Required]
        public int Points { get; set; }

        [Required]
        public int TotPoints { get; set; }

        [Required]
        public string Category { get; set; }

        // Foreign key (convenzione di EF Core)
        [ForeignKey(nameof(RaceSummary))]
        public int RaceSummaryId { get; set; }  // Nome corretto per convenzione

        // Relazione inversa (obbligatoria per EF Core)
        public DbRaceSummary RaceSummary { get; set; }  // Nome corretto
    }
}