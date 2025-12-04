using OfficeOpenXml.Export.HtmlExport.StyleCollectors.StyleContracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class Order
	{

		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Order Date")]
		public DateTime OrderDate { get; set; }

		[Required]
		[Display(Name = "Total Amount")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal TotalAmount { get; set; }

		[Required]
		public int UserId { get; set; }

		[Required]
		[Display(Name = "Order Status")]
		[StringLength(100)]
		public string OrderStatus { get; set; } = "Pending";

		// New field to store Dealer ID
		public int DealerId { get; set; }

		// Navigation properties
		[ForeignKey("UserId")]
		public virtual User User { get; set; }

		public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
	}
}
