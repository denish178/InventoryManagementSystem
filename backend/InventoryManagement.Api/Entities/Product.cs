namespace InventoryManagement.Api.Entities;

public class Product
{
    public int Id { get; set; }

    public string SKU { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal UnitPrice { get; set; }

    public int QuantityInStock { get; set; }

    public int ReorderLevel { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int SupplierId { get; set; }

    public Supplier Supplier { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}