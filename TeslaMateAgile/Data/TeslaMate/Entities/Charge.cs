using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeslaMateAgile.Data.TeslaMate.Entities
{
    [Table("charges")]
    public class Charge
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [NotMapped]
#pragma warning disable CS0618 // Type or member is obsolete
        public DateTimeOffset Date { get => new DateTimeOffset(DateInternal, TimeSpan.Zero); set => DateInternal = value.UtcDateTime; }
#pragma warning restore CS0618 // Type or member is obsolete

        [Column("date")]
        [Obsolete("Do not use this internal property directly, use Date instead")]
        // This property is UTC but is read out by the database as a local date, so we must force it to be UTC using the getter above
        public DateTime DateInternal { get; set; }

        [Column("charge_energy_added")]
        public decimal ChargeEnergyAdded { get; set; }

        [Column("charger_phases")]
        public int? ChargerPhases { get; set; }

        [Column("charger_power")]
        public int ChargerPower { get; set; }

        [Column("charger_actual_current")]
        public int? ChargerActualCurrent { get; set; }

        [Column("charger_voltage")]
        public int? ChargerVoltage { get; set; }

        [ForeignKey("charging_process_id")]
        public ChargingProcess ChargingProcess { get; set; }
    }
}
