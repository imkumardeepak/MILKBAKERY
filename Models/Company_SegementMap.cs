using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class Company_SegementMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Company Name")]
        public string Companyname { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Segement Name")]
        public string Segementname { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Compant Code")]
        public string companycode { get; set; }

        [Required]
        [MaxLength(255)]
        [Display(Name = "Segement Code")]
        public string segementcode { get; set; }


        [StringLength(500)]
        [Display(Name = "Remarks")]

        public string remarks { get; set; } = "NA";
    }
}
