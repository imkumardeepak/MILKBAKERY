using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
	public class DeliveredQuantityReportViewModel
	{
		public List<DeliveredQuantityReportItem> ReportItems { get; set; } = new List<DeliveredQuantityReportItem>();
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public string CustomerName { get; set; }
		public int? CustomerId { get; set; }
		public List<string> AvailableCustomers { get; set; } = new List<string>();
		public bool ShowOnlyVariance { get; set; } = false;
	}

	public class DeliveredQuantityReportItem
	{
		public string CustomerName { get; set; }
		public string DealerName { get; set; }
		public DateTime OrderDate { get; set; }
		public int OrderId { get; set; }
		public string MaterialName { get; set; }
		public string ShortCode { get; set; }
		public int OrderedQuantity { get; set; }
		public int DeliveredQuantity { get; set; }
		public int QuantityVariance { get; set; }
		public decimal UnitPrice { get; set; }
		public decimal OrderedAmount { get; set; }
		public decimal DeliveredAmount { get; set; }
		public decimal AmountVariance { get; set; }
		public bool HasVariance => QuantityVariance != 0 || AmountVariance != 0;
	}
}