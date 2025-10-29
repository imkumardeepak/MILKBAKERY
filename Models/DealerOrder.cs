using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class DealerOrder
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Order Date")]
		[Column(TypeName = "date")]
		public DateTime OrderDate { get; set; }

		[Required]
		[Display(Name = "Distributor ID")]
		public int DistributorId { get; set; }

		[Required]
		[Display(Name = "Dealer ID")]
		public int DealerId { get; set; }

		[Required]
		[StringLength(50)]
		[Display(Name = "Distributor Code")]
		public string? DistributorCode { get; set; }

		[Required]
		[Display(Name = "Process Flag")]
		public int ProcessFlag { get; set; }

		[Display(Name = "Deliver Flag")]
		public int DeliverFlag { get; set; } = 0;

		// Navigation property for related dealer
		[ForeignKey("DealerId")]
		public virtual DealerMaster Dealer { get; set; }

		// Navigation property for related order items
		public virtual ICollection<DealerOrderItem> DealerOrderItems { get; set; } = new List<DealerOrderItem>();

	}
}