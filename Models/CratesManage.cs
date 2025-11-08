using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class CratesManage
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Customer Id")]
		public int CustomerId { get; set; }

		[Required]
		[StringLength(50)]
		[Display(Name = "Segment Code")]
		public string SegmentCode { get; set; }

		[Required]
		[DataType(DataType.Date)]
		[Display(Name = "Dispatch Date")]
		[Column(TypeName = "Date")]
		public DateTime DispDate { get; set; } = DateTime.Now.Date;

		[Required]
		[Display(Name = "Opening")]
		public int Opening { get; set; }

		[Required]
		[Display(Name = "Outward")]
		public int Outward { get; set; } = 0;

		[Required]
		[Display(Name = "Inward")]
		public int Inward { get; set; } = 0;

		[Required]
		[Display(Name = "Balance")]
		public int Balance { get; set; }

		[Display(Name = "Crates Type")]
		public int? CratesTypeId { get; set; }

		// Navigation properties
		[ForeignKey("CustomerId")]
		public virtual Customer_Master? Customer { get; set; }
		
		[ForeignKey("CratesTypeId")]
		public virtual CratesType? CratesType { get; set; }
	}
}