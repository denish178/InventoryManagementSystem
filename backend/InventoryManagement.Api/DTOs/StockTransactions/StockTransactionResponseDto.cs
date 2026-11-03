
namespace InventoryManagement.Api.DTOs.StockTransactions;

public class StockTransactionResponseDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string SKU { get; set; } = string.Empty;

    public string TransactionType { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }
}
