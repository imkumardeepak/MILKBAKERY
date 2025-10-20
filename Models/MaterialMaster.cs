using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class MaterialMaster
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Short Name")]
		public string ShortName { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Material Name")]
		public string Materialname { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Unit")]
		public string Unit { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Category")]
		public string Category { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Sub-Category")]
		public string subcategory { get; set; }

		[Required]
		[Display(Name = "Sequence")]
		public int sequence { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Segement Name")]
		public string segementname { get; set; }

		[Required]
		[StringLength(500)]
		[Display(Name = "Material 3Party Code")]
		public string material3partycode { get; set; }

		[Required]
		[Display(Name = "Price")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal price { get; set; }

		[Required]
		[Display(Name = "Active")]
		public bool isactive { get; set; }

		[Required(ErrorMessage = "A Crates Type is required.")]
		[Display(Name = "Crates Type")]
		public string CratesTypes { get; set; }

		[Display(Name = "Dealer Price")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal dealerprice { get; set; } = 1;
	}
}