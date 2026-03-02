using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class Month
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Name { get; set; } = null!; // Ocak, Şubat, vb.

        [Required]
        public int MonthNumber { get; set; } // 1-12

        public int Order { get; set; } // Sıralama için
    }
}


