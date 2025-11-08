using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class DealerOrderItem
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Dealer Order ID")]
		public int DealerOrderId { get; set; }

		[Required]
		[StringLength(200)]
		[Display(Name = "Material Name")]
		public string MaterialName { get; set; }

		[Required]
		[StringLength(10)]
		[Display(Name = "Short Code")]
		public string ShortCode { get; set; }

		[Required]
		[StringLength(50)]
		[Display(Name = "SAP Code")]
		public string SapCode { get; set; }

		[Required]
		[Display(Name = "Quantity")]
		public int Qty { get; set; }

		[Required]
		[Column(TypeName = "decimal(18,2)")]
		[Display(Name = "Rate")]
		public decimal Rate { get; set; }

		[Display(Name = "Deliver Quantity")]
		[Range(0, int.MaxValue, ErrorMessage = "Delivered quantity cannot be negative")]
		public int DeliverQnty { get; set; } = 0;


		// Navigation property to DealerOrder
		[ForeignKey("DealerOrderId")]
		public virtual DealerOrder DealerOrder { get; set; }
	}
}