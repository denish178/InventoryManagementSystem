namespace InventoryManagement.Api.Entities;

public class StockTransaction
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string TransactionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }
}