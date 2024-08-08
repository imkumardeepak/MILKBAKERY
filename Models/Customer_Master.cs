using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class Customer_Master
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Short Name")]
        public string shortname { get; set; }

        [Required]
        [StringLength(500)]
        [Display(Name = "Name")]
        public string Name { get; set; }



        [Required]
        [StringLength(500)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }


        [Display(Name = "Division")]
        public string Division { get; set; }


        [Required]
        [Display(Name = "Account Type")]
        public string accounttype { get; set; }


        [Required]
        [Display(Name = "Route")]
        public string route { get; set; }


        [Required]
        [Display(Name = "Address")]
        public string address { get; set; }

        [Required]
        [MaxLength(10),MinLength(10)]
        [Phone]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\d{0,10}$", ErrorMessage = "Phone number should have a maximum of 10 digits.")]
        [Display(Name = "Register Phone No")]
        public string phoneno { get; set; }


        public string email { get; set; } = "NA";

      
        [Display(Name = "City")]
        public string city { get; set; }

    
        [Display(Name = "Pin Code")]
        [MaxLength(6, ErrorMessage = "Invalid PinCode")]
        [MinLength(6)]
        public string PostalCode { get; set; }

      
        [Display(Name = "Country")]
        public string country { get; set; }

    
        [Display(Name = "State")]
        public string state { get; set; }



    }
}
