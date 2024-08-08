using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }


        [Display(Name = "Order No")]
        public string OrderNo { get; set; } = "NA";

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; } = DateTime.Now.Date;

        [Required]
        [StringLength(500)]
        [Display(Name = "Customer Name")]
        public string Customername { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Customer Code")]
        public string CustomerCode { get; set; } 

        [Required]
        [Display(Name = "company Name")]
        public string companycode { get; set; } = "1";

        [Required]
        [StringLength(500)]
        [Display(Name = "Segement Name")]
        public string Segementname { get; set; }


        [Required]
        [StringLength(500)]
        [Display(Name = "Segement Code")]
        public string Segementcode { get; set; }

        public int verifyflag { get; set; } = 0;


        public int processflag { get; set; } = 0;

        public virtual List<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();

        [NotMapped]
        public bool IsSelected { get; set; }

    }
}
