using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class UnitMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        public string UnitName { get; set; }

        [Required]
        [StringLength(500)]
        public string Unit_Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string Precision { get; set; }

    }
}
