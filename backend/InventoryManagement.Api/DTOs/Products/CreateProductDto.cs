
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs.Products;

public class CreateProductDto
{
    [Required]
    [StringLength(50, MinimumLength = 2)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal UnitPrice { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }

    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int SupplierId { get; set; }
}
