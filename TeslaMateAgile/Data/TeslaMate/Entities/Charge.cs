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

        [Column("date")]
        public DateTime Date { get; set; }

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
