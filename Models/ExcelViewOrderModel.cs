using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Milk_Bakery.Models
{
    public class ExcelViewOrderModel
    {
        public int dealerId { get; set; }
        public List<ExcelViewOrderItem> orderItems { get; set; }
    }

    public class ExcelViewOrderItem
    {
        public int MaterialId { get; set; }
        public int Quantity { get; set; }
    }
}