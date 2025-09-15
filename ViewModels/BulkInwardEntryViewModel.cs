using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class BulkInwardCustomerViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int Inward { get; set; }
    }

    public class BulkInwardEntryViewModel
    {
        [Required]
        [Display(Name = "Segment")]
        public string SegmentCode { get; set; }

        [Required]
        [Display(Name = "Crates Type")]
        public int CratesTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Dispatch Date")]
        public System.DateTime DispDate { get; set; }

        public List<BulkInwardCustomerViewModel> Customers { get; set; }

        public BulkInwardEntryViewModel()
        {
            Customers = new List<BulkInwardCustomerViewModel>();
            DispDate = System.DateTime.Now;
        }
    }
}