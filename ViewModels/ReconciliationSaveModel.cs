using System;
using System.Collections.Generic;

namespace Milk_Bakery.ViewModels
{
    public class ReconciliationSaveModel
    {
        public string Date { get; set; }
        public int CustomerId { get; set; }
        public List<ReconciliationAdjustment> Adjustments { get; set; } = new List<ReconciliationAdjustment>();
    }
    
    public class ReconciliationAdjustment
    {
        public string MaterialName { get; set; }
        public string ShortCode { get; set; }
        public int OrderedCrates { get; set; }
        public int ReceivedCrates { get; set; }
        public int ItemsPerCrate { get; set; }
    }
}