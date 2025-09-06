using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.ViewModels
{
    /// <summary>
    /// A simple DTO for displaying material information without triggering validation
    /// </summary>
    public class MaterialDisplayModel
    {
        public int Id { get; set; }
        
        public string? ShortName { get; set; }
        
        public string? Materialname { get; set; }
        
        public string? Unit { get; set; }
        
        public string? Category { get; set; }
        
        public string? subcategory { get; set; }
        
        public int sequence { get; set; }
        
        public string? segementname { get; set; }
        
        public string? material3partycode { get; set; }
        
        public decimal price { get; set; }
        
        public bool isactive { get; set; }
        
        public string? CratesCode { get; set; }
    }
}