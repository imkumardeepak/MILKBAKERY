using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
	public class User
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }


		public string? Role { get; set; }

		[Required]
		[Display(Name = "User Name")]
		public string phoneno { get; set; }

		[Required]
		[StringLength(10)]
		[DataType(DataType.Password)]
		[Display(Name = "Password")]
		public string Password { get; set; }

		[NotMapped]
		[Required]
		[Compare("Password", ErrorMessage = "Passwords do not match.")]
		public string? ConfirmPassword { get; set; }

        public string? name { get; set; }
    }
}
