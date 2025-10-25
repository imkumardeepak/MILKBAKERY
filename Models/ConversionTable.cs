using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    [Table("ConversionTable")]
    public class ConversionTable
    {
        [Key]
        [Column("materialname")]
        [StringLength(200)]
        public string MaterialName { get; set; }

        [Required]
        [Column("shortcode")]
        [StringLength(50)]
        public string ShortCode { get; set; }

        [Required]
        [Column("sapcode")]
        [StringLength(100)]
        public string SapCode { get; set; }

        [Required]
        [Column("unittype")]
        [StringLength(50)]
        public string UnitType { get; set; }

        [Column("unitqnty")]
        public int UnitQuantity { get; set; }

        [Column("totalqnty")]
        public int TotalQuantity { get; set; }
    }
}