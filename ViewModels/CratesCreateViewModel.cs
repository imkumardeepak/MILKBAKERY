using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Milk_Bakery.ViewModels
{
    public class CratesCreateViewModel
    {
        public List<CratesEntryViewModel> CratesEntries { get; set; } = new List<CratesEntryViewModel>();
        
        // Filter properties
        public string SelectedSegmentCode { get; set; }
        public List<SelectListItem> Segments { get; set; } = new List<SelectListItem>();
        
        // Flag to indicate if we should show the table
        public bool ShowTable { get; set; } = false;
    }
}