using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class CreditDebitRecord
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Customer Id")]
        public int CustomerId { get; set; }

        [Required]
        [Display(Name = "Crates Type Id")]
        public int CratesId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Segment")]
        public string Segment { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Transaction Date")]
        [Column(TypeName = "Date")]
        public DateTime Date { get; set; } = DateTime.Now.Date;

        [StringLength(500)]
        [Display(Name = "Reason")]
        public string? Reason { get; set; } = "NA";

        [Required]
        [StringLength(100)]
        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        // Navigation properties
        [ForeignKey("CustomerId")]
        public virtual Customer_Master? Customer { get; set; }

        [ForeignKey("CratesId")]
        public virtual CratesType? CratesType { get; set; }
    }
}