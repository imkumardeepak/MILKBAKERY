using System;

namespace Milk_Bakery.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDetails { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string RedirectUrl { get; set; }
        public string RedirectText { get; set; }
    }
}