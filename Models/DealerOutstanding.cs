using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class DealerOutstanding
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Dealer ID")]
        public int DealerId { get; set; }

        [Required]
        [Display(Name = "Delivery Date")]
        [Column(TypeName = "date")]
        public DateTime DeliverDate { get; set; }

        [Required]
        [Display(Name = "Invoice Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal InvoiceAmount { get; set; }

        [Required]
        [Display(Name = "Paid Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal PaidAmount { get; set; }

        [Required]
        [Display(Name = "Balance Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BalanceAmount { get; set; }

        // Navigation property for related dealer
        [ForeignKey("DealerId")]
        public virtual DealerMaster Dealer { get; set; }
    }
}