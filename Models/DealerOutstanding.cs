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
        [Display(Name = "Outstanding Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal OutstandingAmount { get; set; }

        [Required]
        [Display(Name = "Received Amount")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal ReceivedAmount { get; set; }

        // Navigation property for related dealer
        [ForeignKey("DealerId")]
        public virtual DealerMaster Dealer { get; set; }
    }
}