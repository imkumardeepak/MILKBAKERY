using Milk_Bakery.Models;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class DealerOrderViewModel
    {
        public DealerMaster DealerMaster { get; set; } = new DealerMaster();
        
        public List<MaterialDisplayModel> AvailableMaterials { get; set; } = new List<MaterialDisplayModel>();
        
        public List<DealerBasicOrder> DealerBasicOrders { get; set; } = new List<DealerBasicOrder>();
        
        // This will hold the quantities entered by the user for each material
        public Dictionary<int, int> MaterialQuantities { get; set; } = new Dictionary<int, int>();
        
        // This will hold which materials are selected
        public HashSet<int> SelectedMaterialIds { get; set; } = new HashSet<int>();
        
        // This will hold the dealer prices entered by the user for each material
        public Dictionary<int, decimal> MaterialDealerPrices { get; set; } = new Dictionary<int, decimal>();
    }
}