using Milk_Bakery.Models;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class DealerOrdersViewModel
    {
        // Distributor/Customer selection
        public int SelectedDistributorId { get; set; }
        public List<Customer_Master> AvailableDistributors { get; set; } = new List<Customer_Master>();
        
        // Dealer listing
        public List<DealerMaster> Dealers { get; set; } = new List<DealerMaster>();
        
        // Dealer Basic Order Display
        public Dictionary<int, List<DealerBasicOrder>> DealerBasicOrders { get; set; } = new Dictionary<int, List<DealerBasicOrder>>();
        
        // Dealer Order Entry Form
        public Dictionary<int, DealerOrder> DealerOrders { get; set; } = new Dictionary<int, DealerOrder>();
        public Dictionary<int, Dictionary<int, int>> DealerOrderItemQuantities { get; set; } = new Dictionary<int, Dictionary<int, int>>();
        
        // Available materials for selection
        public List<MaterialMaster> AvailableMaterials { get; set; } = new List<MaterialMaster>();
    }
}