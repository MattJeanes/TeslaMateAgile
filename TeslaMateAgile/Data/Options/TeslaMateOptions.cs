using System.ComponentModel.DataAnnotations;
using TeslaMateAgile.Data.Enums;

namespace TeslaMateAgile.Data.Options
{
    public class TeslaMateOptions
    {
        [Range(1, int.MaxValue)]
        public int GeofenceId { get; set; }

        [Range(1, int.MaxValue)]
        public int UpdateIntervalSeconds { get; set; }

        [Range(1, int.MaxValue)]
        public int Phases { get; set; }

        public EnergyProvider EnergyProvider { get; set; }
    }
}