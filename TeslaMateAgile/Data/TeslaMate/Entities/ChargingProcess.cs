using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeslaMateAgile.Data.TeslaMate.Entities
{
    [Table("charging_processes")]
    public class ChargingProcess
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime? EndDate { get; set; }

        [Column("geofence_id")]
        public int GeofenceId { get; set; }

        [Column("cost")]
        public decimal? Cost { get; set; }

        public ICollection<Charge> Charges { get; set; }
    }
}
