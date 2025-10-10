using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class BulkOutwardCustomerViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public int Outward { get; set; }
    }

    public class BulkOutwardEntryViewModel
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

        public List<BulkOutwardCustomerViewModel> Customers { get; set; }

        public BulkOutwardEntryViewModel()
        {
            Customers = new List<BulkOutwardCustomerViewModel>();
            DispDate = System.DateTime.Now;
        }
    }
}