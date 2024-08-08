using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class CompanyMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string ShortName { get; set; }

        [Required]
        public string Division { get; set; }

        [Required]
        public string Address { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string District { get; set; } = "NA";

        [Required]
        public string Country { get; set; }

        [Required]

        public string State { get; set; }

      


 
        public string PinCode { get; set; }



        [DataType(DataType.PhoneNumber)]
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression(@"^\d{0,10}$", ErrorMessage = "Phone number should have a maximum of 10 digits.")]
        public string PhoneNumber { get; set; }



    }
}
