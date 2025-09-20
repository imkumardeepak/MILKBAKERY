using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class PageAccess
    {
        [Key]
        public int Id { get; set; }
        public string PageName { get; set; }
        public bool HasAccess { get; set; }

        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }
    }
}