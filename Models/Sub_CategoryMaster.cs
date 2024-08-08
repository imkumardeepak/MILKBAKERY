using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class Sub_CategoryMaster
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string ShortCode { get; set; }

        [Required]
        [StringLength(500)]
        public string SubCategoryName { get; set; }

        [Required]
        [StringLength(500)]
        public string CategoryName { get; set;}

       
    }
}
