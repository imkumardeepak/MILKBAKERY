using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.DTOs
{
    // DealerBasicOrder response DTO
    public class DealerBasicOrderResponseDto
    {
        public int Id { get; set; }
        public int DealerId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string SapCode { get; set; } = string.Empty;
        public string ShortCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal BasicAmount { get; set; }
    }

    // DealerBasicOrder create request DTO
    public class CreateDealerBasicOrderRequestDto
    {
        [Required(ErrorMessage = "Dealer ID is required")]
        public int DealerId { get; set; }

        [Required(ErrorMessage = "Material Name is required")]
        [StringLength(200, ErrorMessage = "Material Name cannot exceed 200 characters")]
        public string MaterialName { get; set; } = string.Empty;

        [Required(ErrorMessage = "SAP Code is required")]
        [StringLength(50, ErrorMessage = "SAP Code cannot exceed 50 characters")]
        public string SapCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Short Code is required")]
        [StringLength(50, ErrorMessage = "Short Code cannot exceed 50 characters")]
        public string ShortCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Basic Amount is required")]
        public decimal BasicAmount { get; set; }
    }

    // DealerBasicOrder update request DTO
    public class UpdateDealerBasicOrderRequestDto
    {
        [Required(ErrorMessage = "ID is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Dealer ID is required")]
        public int DealerId { get; set; }

        [Required(ErrorMessage = "Material Name is required")]
        [StringLength(200, ErrorMessage = "Material Name cannot exceed 200 characters")]
        public string MaterialName { get; set; } = string.Empty;

        [Required(ErrorMessage = "SAP Code is required")]
        [StringLength(50, ErrorMessage = "SAP Code cannot exceed 50 characters")]
        public string SapCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Short Code is required")]
        [StringLength(50, ErrorMessage = "Short Code cannot exceed 50 characters")]
        public string ShortCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Basic Amount is required")]
        public decimal BasicAmount { get; set; }
    }
}