using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class MaterialImage
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Material Master")]
		public int MaterialMasterId { get; set; }

		[Required]
		public string? ImagePath { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		// Navigation property
		[ForeignKey("MaterialMasterId")]
		public virtual MaterialMaster? MaterialMaster { get; set; }
	}
}
