using System.ComponentModel.DataAnnotations;

namespace TeslaMateAgile.Data.TeslaMate.Entities
{
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
    }
}
