using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class DealerBasicOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer ID")]
        public int DealerId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Material Name")]
        public string MaterialName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "SAP Code")]
        public string SapCode { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Short Code")]
        public string ShortCode { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Basic amount must be greater than 0")]
        [Display(Name = "Basic Amount")]
        public decimal BasicAmount { get; set; }

        // Navigation property
        [ForeignKey("DealerId")]
        public virtual DealerMaster DealerMaster { get; set; }
    }
}