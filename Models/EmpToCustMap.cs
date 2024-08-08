using DocumentFormat.OpenXml.Wordprocessing;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.Models
{
    public class EmpToCustMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }


        [Required]
        [StringLength(50)]
        [Display(Name = "Employee")]

        public string empl { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "PhoneNo")]
        public string phoneno { get; set; }


        public virtual List<Cust2EmpMap> Cust2EmpMaps { get; set; } = new List<Cust2EmpMap>();
    }
    public class Cust2EmpMap
    {

        public int id { get; set; }

        [ForeignKey("EmpToCustMaps")]
        public int empt2custid { get; set; }
        public virtual EmpToCustMap? EmpToCustMaps { get; private set; }

        [Required]
        [StringLength(50)]
        public string customer { get; set; }

        [Required]
        [StringLength(50)]
        public string phone { get; set; }

        [NotMapped]
        public bool IsDeleted { get; set; } = false;

    }
}
