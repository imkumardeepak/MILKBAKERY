using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class EmployeeMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string EmpCode { get; set; }

        [Required]
        [StringLength(500)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(500)]
        public string MiddileName { get; set; }

        [Required]
        [StringLength(500)]
        public string LastName { get; set; }

        [Required]
        [StringLength(500)]
        public string Department { get; set; }


        [Required]
        [StringLength(500)]
        public string Route { get; set; }

        [Required]
        [StringLength(500)]
        public string Segment { get; set; } = "BAKERY DIVISION";


        [Required]
        [StringLength(500)]
        public string Grade { get; set; }

        [Required]
        [StringLength(500)]
        public string UserType { get; set; }

        [Required]
        [StringLength(500)]
        public string Designation { get; set; }

        [Required]
        [StringLength(500)]
        public string EmployeeType { get; set; }

        [Required]
        [StringLength(10)]
        [DataType(DataType.PhoneNumber)]
        [MinLength(10)]
        [MaxLength(10)]
        [RegularExpression(@"^\d{0,10}$", ErrorMessage = "Phone number should have a maximum of 10 digits.")]
        public string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool isActive { get; set; }

    }
}
