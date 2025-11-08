using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.Models
{
	public class Role
	{
		[Key]
		public int Id { get; set; }
		public string RoleName { get; set; }

		public virtual ICollection<PageAccess> PageAccesses { get; set; }
	}
}