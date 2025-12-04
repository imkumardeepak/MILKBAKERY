using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class PointsRewardCampaign
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Campaign Name")]
		public string CampaignName { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Start Date")]
		public DateTime StartDate { get; set; }

		[Required]
		[Display(Name = "End Date")]
		public DateTime EndDate { get; set; }

		[Display(Name = "Description")]
		public string? Description { get; set; }

		[Required]
		[Display(Name = "Default Voucher Generation Threshold (Points)")]
		public int VoucherGenerationThreshold { get; set; }

		[Required]
		[Display(Name = "Voucher Validity (Days)")]
		public int VoucherValidity { get; set; }

		[Display(Name = "Materials")]
		public string? Materials { get; set; } // Comma-separated list of material IDs

		[Display(Name = "Material Points")]
		public string? MaterialPoints { get; set; } // JSON string of material ID to points mapping

		// New property for product selection
		[Display(Name = "Reward Product")]
		public int? RewardProductId { get; set; }

		// Navigation property
		[ForeignKey("RewardProductId")]
		public virtual Product? RewardProduct { get; set; }

		[Display(Name = "Sales Voucher Value (₹)")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal SalesVoucherValue { get; set; } = 0;

		[Display(Name = "Distributor Voucher Value (₹)")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal DistributorVoucherValue { get; set; } = 0;

		[Display(Name = "Campaign Image")]
		public string? ImagePath { get; set; }

		public bool IsActive { get; set; } = true;
	}
}
