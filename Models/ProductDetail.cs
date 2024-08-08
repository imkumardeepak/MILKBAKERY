using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.Models
{
    public class ProductDetail
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("PurchaseOrder")]
        public int PurchaseOrderId { get; set; }
        public virtual PurchaseOrder? PurchaseOrder { get; private set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Product")]

        public string ProductName { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Product Code")]
        public string ProductCode { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Unit")]
        public string Unit { get; set; }

        [Required]
        [Display(Name = "Quntity")]

        public int qty { get; set; } = 0;

        [Required]
        [Display(Name = "Rate")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Rate { get; set; }

        [Required]
        [Display(Name = "Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [NotMapped]
        public bool IsDeleted { get; set; } = false;

        [NotMapped]
        public decimal Total { get; set; }
    }
}
