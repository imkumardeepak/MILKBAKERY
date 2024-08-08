using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Milk_Bakery.Models
{
    public class Cust2CustMap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }


        [Required]
        [StringLength(50)]
        [Display(Name = "Cutomer")]

        public string custname { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "PhoneNo")]
        public string phoneno { get; set; }


        public virtual List<Mappedcust> Mappedcusts { get; set; } = new List<Mappedcust>();
    }

    public class Mappedcust
    {

        public int id { get; set; }

        [ForeignKey("Cust2CustMaps")]
        public int cust2custId { get; set; }
        public virtual Cust2CustMap Cust2CustMaps { get; private set; }

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
