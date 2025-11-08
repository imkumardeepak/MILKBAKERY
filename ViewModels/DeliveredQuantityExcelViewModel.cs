using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Milk_Bakery.Models;

namespace Milk_Bakery.ViewModels
{
    public class DeliveredQuantityExcelViewModel
    {
        // Distributor/Customer selection
        public int SelectedDistributorId { get; set; }
        public List<Customer_Master> AvailableDistributors { get; set; } = new List<Customer_Master>();

        // Dealer listing
        public List<DealerMaster> Dealers { get; set; } = new List<DealerMaster>();

        // Dealer Orders
        public Dictionary<int, List<DealerOrder>> DealerOrders { get; set; } = new Dictionary<int, List<DealerOrder>>();

        // Available materials for selection
        public List<MaterialMaster> AvailableMaterials { get; set; } = new List<MaterialMaster>();

        // Conversion data for crate calculations
        public Dictionary<string, ConversionTable> MaterialConversions { get; set; } = new Dictionary<string, ConversionTable>();
    }
}