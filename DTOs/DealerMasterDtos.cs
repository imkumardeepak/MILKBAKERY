using System.ComponentModel.DataAnnotations;

namespace Milk_Bakery.DTOs
{
    // DealerMaster response DTO
    public class DealerMasterResponseDto
    {
        public int Id { get; set; }
        public int DistributorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = "N/A";
        public List<DealerBasicOrderResponseDto> DealerBasicOrders { get; set; } = new List<DealerBasicOrderResponseDto>();
    }

    // DealerMaster create request DTO
    public class CreateDealerMasterRequestDto
    {
        [Required(ErrorMessage = "Distributor ID is required")]
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Route Code is required")]
        [StringLength(50, ErrorMessage = "Route Code cannot exceed 50 characters")]
        public string RouteCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone No is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number should have exactly 10 digits")]
        public string PhoneNo { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = "N/A";
    }

    // DealerMaster update request DTO
    public class UpdateDealerMasterRequestDto
    {
        [Required(ErrorMessage = "ID is required")]
        public int Id { get; set; }

        [Required(ErrorMessage = "Distributor ID is required")]
        public int DistributorId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Route Code is required")]
        [StringLength(50, ErrorMessage = "Route Code cannot exceed 50 characters")]
        public string RouteCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required")]
        [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City cannot exceed 100 characters")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone No is required")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Phone number should have exactly 10 digits")]
        public string PhoneNo { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = "N/A";
    }

    // DealerMaster list item DTO
    public class DealerMasterListItemDto
    {
        public int Id { get; set; }
        public int DistributorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string RouteCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string Email { get; set; } = "N/A";
        public int OrderCount { get; set; }
    }
}