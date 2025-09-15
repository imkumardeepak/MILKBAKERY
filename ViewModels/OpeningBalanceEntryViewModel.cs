using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    public class OpeningBalanceCustomerViewModel
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }

        [Display(Name = "Opening Balance")]
        [Range(0, int.MaxValue, ErrorMessage = "Opening balance must be a non-negative number.")]
        public int OpeningBalance { get; set; }
    }

    public class OpeningBalanceEntryViewModel
    {
        [Required(ErrorMessage = "Please select a segment.")]
        [Display(Name = "Segment")]
        public string? SegmentCode { get; set; }

        [Required(ErrorMessage = "Please select a crates type.")]
        [Display(Name = "Crates Type")]
        public int CratesTypeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Dispatch Date")]
        public DateTime DispDate { get; set; }

        public List<OpeningBalanceCustomerViewModel> Customers { get; set; }

        public OpeningBalanceEntryViewModel()
        {
            Customers = new List<OpeningBalanceCustomerViewModel>();
            DispDate = DateTime.Today;
        }
    }
}