using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class SegementMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Short Name")]
        public string ShortName { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Segement Name")]
        public string SegementName { get; set; }

        [Required]
        [MaxLength(50)]
        [Display(Name = "Segement Code 3rd Party")]
        public string Segement_Code { get; set; }
    }
}
