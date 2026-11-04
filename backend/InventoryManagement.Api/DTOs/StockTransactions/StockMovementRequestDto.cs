using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs.StockTransactions;

public class StockMovementRequestDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be a whole number greater than zero.")]
    public int Quantity { get; set; }

    [StringLength(100)]
    public string? ReferenceNumber { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
