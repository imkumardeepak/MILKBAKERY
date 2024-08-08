using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class CustomerSegementMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Customer Name")]
        public string Customername { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name ="Segement Name")]
        public string SegementName { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name ="Customer Segement Code")]
        public string custsegementcode { get; set;}

        [Required]
        [StringLength(500)]
        [Display(Name = "Segement Code 3rd Party")]
        public string segementcode3party { get; set; }


        [StringLength(500)]
        [Display(Name = "Remarks")]

        public string remarks { get; set; } = "NA";


        [NotMapped]
        public string shortcode { get; set; }



    }
}
