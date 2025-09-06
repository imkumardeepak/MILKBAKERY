using Milk_Bakery.Models;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class DealerOrdersViewModel
    {
        public List<DealerMaster> Dealers { get; set; }
        public List<MaterialMaster> Materials { get; set; }
        public Dictionary<int, List<DealerOrderDetailViewModel>> DealerOrderDetails { get; set; }
    }

    public class DealerOrderDetailViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; }
        public int Quantity { get; set; }
    }
}