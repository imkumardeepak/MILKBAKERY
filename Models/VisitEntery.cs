using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class VisitEntery
    {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Salesname { get; set; }

        [Required]
        public string CustnameName { get; set; }

        [Required]
        public string route { get; set; }

        [Required]
        public string city { get; set; }

        [Required]
        public DateTime dateTime { get; set; } = DateTime.Now;

        [Required]
        public string longitute { get; set; }

        [Required]
        public string latitude { get; set; }

        [Required]
        public string location { get; set; }

        [Required]
        public string retailer { get; set; }

        public string remarkss { get; set; } = "NA";
    }
}
