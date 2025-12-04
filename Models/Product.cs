using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class Product
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Product Name")]
		public string ProductName { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Price")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal Price { get; set; }

		[Required]
		[Display(Name = "Category")]
		public string Category { get; set; } = string.Empty;

		[Display(Name = "Description")]
		public string Description { get; set; } = string.Empty;

		[Display(Name = "Is Active")]
		public bool IsActive { get; set; } = true;
	}
}
