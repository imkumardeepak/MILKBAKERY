using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.Models
{
	public class Poster
	{
		public int Id { get; set; }

		[Required]
		public string? ImagePath { get; set; }

		[Required]
		public string? Message { get; set; }

		[Required]
		[Display(Name = "Show From")]
		public DateTime ShowFrom { get; set; }

		[Required]
		[Display(Name = "Show Until")]
		public DateTime ShowUntil { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	}
}
