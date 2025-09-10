using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Milk_Bakery.Models
{
	public class DealerMaster
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Distributor ID")]
		public int DistributorId { get; set; }

		[Required]
		[StringLength(200)]
		[Display(Name = "Name")]
		public string Name { get; set; }

		[Required]
		[StringLength(50)]
		[Display(Name = "Route Code")]
		public string RouteCode { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Address")]
		public string Address { get; set; }

		[Required]
		[StringLength(100)]
		[Display(Name = "City")]
		public string City { get; set; }

		[Required]
		[MaxLength(10), MinLength(10)]
		[Phone]
		[DataType(DataType.PhoneNumber)]
		[RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number should have exactly 10 digits.")]
		[Display(Name = "Phone No")]
		public string PhoneNo { get; set; }

		[StringLength(100)]
		[Display(Name = "Email")]
		public string? Email { get; set; } = "";

		// Navigation property for related basic orders
		public virtual ICollection<DealerBasicOrder> DealerBasicOrders { get; set; } = new List<DealerBasicOrder>();
		
		
	}
}