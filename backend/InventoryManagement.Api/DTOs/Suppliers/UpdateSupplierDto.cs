using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs.Suppliers;

public class UpdateSupplierDto
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContactPerson { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;
}