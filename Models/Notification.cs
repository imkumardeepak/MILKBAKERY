using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class Notification
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "User ID")]
		public int UserId { get; set; }

		[Required]
		[Display(Name = "Title")]
		public string Title { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Message")]
		public string Message { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Notification Type")]
		public string Type { get; set; } = string.Empty; // campaign, order, voucher

		[Required]
		[Display(Name = "Created Date")]
		public DateTime CreatedDate { get; set; }

		[Display(Name = "Is Read")]
		public bool IsRead { get; set; } = false;

		[Display(Name = "Related Entity ID")]
		public int? RelatedEntityId { get; set; }

		[Display(Name = "Related Entity Type")]
		public string? RelatedEntityType { get; set; } // Campaign, Order, Voucher

		// Navigation properties
		[ForeignKey("UserId")]
		public virtual User User { get; set; }

		public Notification()
		{
			CreatedDate = DateTime.UtcNow;
		}
	}
}
