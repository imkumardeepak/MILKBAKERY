using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class CratesOverviewViewModel
    {
        public List<CratesEntryViewModel> CratesEntries { get; set; } = new List<CratesEntryViewModel>();
    }

    public class CratesEntryViewModel
    {
        public int CustomerId { get; set; }
        
        public string CustomerName { get; set; } = string.Empty;

        public string SegmentCode { get; set; } = string.Empty;
        
        public string SegmentName { get; set; } = string.Empty;

        public int CrateTypeId { get; set; }
        
        public string CrateTypeName { get; set; } = string.Empty;

        public int Opening { get; set; } = 0;

        public int Outward { get; set; } = 0;

        public int Inward { get; set; } = 0;

        public int Balance { get; set; } = 0;
    }
}