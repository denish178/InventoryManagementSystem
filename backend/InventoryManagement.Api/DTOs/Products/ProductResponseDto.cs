
namespace InventoryManagement.Api.DTOs.Products;

public class ProductResponseDto
{
    public int Id { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal UnitPrice { get; set; }
    public int QuantityInStock { get; set; }
    public int ReorderLevel { get; set; }

    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
