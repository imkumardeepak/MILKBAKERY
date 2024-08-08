using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class PlantMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required]
        public string Shortcode { get; set; }

        [Required]
        public string SegmentCode { get; set; }

        [Required]
        public string Description { get; set; }

    }
}
