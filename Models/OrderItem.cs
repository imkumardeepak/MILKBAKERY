using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class OrderItem
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		public int OrderId { get; set; }

		[Required]
		public int MaterialId { get; set; }

		[Required]
		[Display(Name = "Quantity")]
		public int Quantity { get; set; }

		[Required]
		[Display(Name = "Unit Price")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal UnitPrice { get; set; }

		[Required]
		[Display(Name = "Total Price")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal TotalPrice { get; set; }

		// New property to store points for this material in the order
		[Required]
		[Display(Name = "Points")]
		public int Points { get; set; } = 0;

		// Navigation properties
		[ForeignKey("OrderId")]
		public virtual Order Order { get; set; }

		[ForeignKey("MaterialId")]
		public virtual MaterialMaster Material { get; set; }
	}
}
