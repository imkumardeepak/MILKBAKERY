using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class SalesVoucher
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required]
		[Display(Name = "Voucher Code")]
		public string VoucherCode { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Sales Person ID")]
		public int SalesPersonId { get; set; }

		[Required]
		[Display(Name = "Campaign Type")]
		public string CampaignType { get; set; } = string.Empty;

		[Required]
		[Display(Name = "Campaign ID")]
		public int CampaignId { get; set; }

		[Required]
		[Display(Name = "Voucher Value")]
		[Column(TypeName = "decimal(18,2)")]
		public decimal VoucherValue { get; set; }

		[Required]
		[Display(Name = "Issue Date")]
		public DateTime IssueDate { get; set; }

		[Required]
		[Display(Name = "Expiry Date")]
		public DateTime ExpiryDate { get; set; }

		[Display(Name = "Is Redeemed")]
		public bool IsRedeemed { get; set; } = false;

		[Display(Name = "Redemption Date")]
		public DateTime? RedeemedDate { get; set; }

		[Display(Name = "QR Code Data")]
		public string? QRCodeData { get; set; }

		public string? Redeemedby { get; set; }

		// Navigation properties
		[ForeignKey("SalesPersonId")]
		public virtual EmployeeMaster? SalesPerson { get; set; }

		public SalesVoucher()
		{
			IssueDate = DateTime.UtcNow;
		}
	}
}
