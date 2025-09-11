using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class VarianceOrderReportViewModel
    {
        public List<VarianceReportItem> ReportItems { get; set; } = new List<VarianceReportItem>();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string CustomerName { get; set; }
        public List<string> AvailableCustomers { get; set; } = new List<string>();
        public bool ShowOnlyVariance { get; set; } = false;
    }

    public class VarianceReportItem
    {
        public string CustomerName { get; set; }
        public string MaterialName { get; set; }
        public string MaterialCode { get; set; }
        public int OrderedQuantity { get; set; }
        public int InvoicedQuantity { get; set; }
        public int QuantityVariance { get; set; }
        public decimal OrderedAmount { get; set; }
        public decimal InvoicedAmount { get; set; }
        public decimal AmountVariance { get; set; }
        
        public bool HasVariance => QuantityVariance != 0 || AmountVariance != 0;
    }
}