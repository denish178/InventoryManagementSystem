
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs.StockTransactions;

public class CreateStockTransactionDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Required]
    [RegularExpression("^(StockIn|StockOut)$",
        ErrorMessage = "TransactionType must be StockIn or StockOut.")]
    public string TransactionType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
