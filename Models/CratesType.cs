using System.ComponentModel.DataAnnotations;

public class CratesType
{
	public int Id { get; set; }

	[Required(ErrorMessage = "The Cratestype name is required.")]
	[StringLength(50, ErrorMessage = "Cratestype cannot be longer than 50 characters.")]
	[Display(Name = "Crates Type Name")]
	public string Cratestype { get; set; }

	[Required(ErrorMessage = "A Crates Code is required.")]
	[Display(Name = "Crates Code")]
	[StringLength(1, MinimumLength = 1, ErrorMessage = "Crates Code must be exactly 1 character.")]
	public string CratesCode { get; set; }

	[Required(ErrorMessage = "Width is required.")]
	[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Please enter a valid width (e.g., 10 or 10.50).")]
	[Display(Name = "Width (in cm)")]
	public string Width { get; set; }

	[Required(ErrorMessage = "Height is required.")]
	[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Please enter a valid height (e.g., 10 or 10.50).")]
	[Display(Name = "Height (in cm)")]
	public string Height { get; set; }

	[Required(ErrorMessage = "Length is required.")]
	[RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = "Please enter a valid length (e.g., 10 or 10.50).")]
	[Display(Name = "Length (in cm)")]
	public string Length { get; set; }
}