using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class CustTransaction
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Company")]

        public string cmpcode { get; set; }

        [Display(Name = "Company Name")]

        public string cmpname { get; set; } = "na";

        [Display(Name = "Customer")]
        public string partycode { get; set; }

        [Display(Name = "Customer Name")]
        public string customername { get; set; } = "na";

        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime edate { get; set; }

        [Display(Name = "Outstanding Amount")]

        [Column(TypeName = "decimal(18,2)")]
        public decimal outstandingampunt { get; set; }

        [Display(Name = "Invoice Amount")]

        [Column(TypeName = "decimal(18,2)")]
        public decimal invoiceamount { get; set; }


        [Display(Name = "Paid Amount")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal recipectamount { get; set; }

        [Display(Name = "Last Update")]

        public DateTime lastupdate { get; set; } = DateTime.Now;

    }
}
