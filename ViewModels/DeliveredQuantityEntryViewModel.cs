using System.ComponentModel.DataAnnotations;
using Milk_Bakery.Models;
using System.Collections.Generic;

namespace Milk_Bakery.ViewModels
{
    public class DeliveredQuantityEntryViewModel
    {
        // Distributor/Customer selection
        public int SelectedDistributorId { get; set; }
        public List<Customer_Master> AvailableDistributors { get; set; } = new List<Customer_Master>();
        
        // Dealer listing
        public List<DealerMaster> Dealers { get; set; } = new List<DealerMaster>();
        
        // Dealer Orders
        public Dictionary<int, List<DealerOrder>> DealerOrders { get; set; } = new Dictionary<int, List<DealerOrder>>();
        
        // Existing properties for entry page
        public int OrderId { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        public string DistributorName { get; set; }
        
        public string DealerName { get; set; }

        public List<DeliveredQuantityItemViewModel> Items { get; set; } = new List<DeliveredQuantityItemViewModel>();
        
        public decimal GrandTotal { get; set; }
    }

    public class DeliveredQuantityItemViewModel
    {
        public int ItemId { get; set; }
        
        [Display(Name = "Item Name")]
        public string ItemName { get; set; }
        
        [Display(Name = "Short Code")]
        public string ShortCode { get; set; }
        
        [Display(Name = "Ordered Quantity")]
        public int OrderedQuantity { get; set; }
        
        [Display(Name = "Delivered Quantity")]
        [Range(0, int.MaxValue, ErrorMessage = "Delivered quantity cannot be negative")]
        public int DeliveredQuantity { get; set; }
        
        [Display(Name = "Variance")]
        public int Variance { get; set; }
        
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }
        
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }
        
        [Display(Name = "Amount Variance")]
        public decimal AmountVariance { get; set; }
    }
}