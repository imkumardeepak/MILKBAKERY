using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.Models
{
    public class MenuItem
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Controller { get; set; }
        public string? Action { get; set; }
        public string? Url { get; set; }
        public int? ParentId { get; set; }
    }
}