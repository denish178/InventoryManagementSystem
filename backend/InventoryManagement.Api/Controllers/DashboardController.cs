
using InventoryManagement.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Inventory Staff,Viewer")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var activeProducts = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        var totalProducts = await activeProducts.CountAsync();

        var lowStockProducts = await activeProducts
            .CountAsync(p => p.QuantityInStock <= p.ReorderLevel);

        var totalUnitsInStock = await activeProducts
            .SumAsync(p => (long)p.QuantityInStock);

        var inventoryValue = await activeProducts
            .SumAsync(p => (decimal?)p.UnitPrice * p.QuantityInStock) ?? 0m;

        var lowStock = await activeProducts
            .Where(p => p.QuantityInStock <= p.ReorderLevel)
            .OrderBy(p => p.QuantityInStock)
            .Take(6)
            .Select(p => new
            {
                p.Id,
                p.SKU,
                p.Name,
                p.QuantityInStock,
                p.ReorderLevel
            })
            .ToListAsync();

        var recentMovements = await _context.StockTransactions
            .AsNoTracking()
            .OrderByDescending(t => t.CreatedAt)
            .Take(8)
            .Select(t => new
            {
                t.Id,
                t.ProductId,
                ProductName = t.Product.Name,
                SKU = t.Product.SKU,
                t.TransactionType,
                t.Quantity,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            TotalProducts = totalProducts,
            LowStockProducts = lowStockProducts,
            TotalUnitsInStock = totalUnitsInStock,
            InventoryValue = inventoryValue,
            LowStock = lowStock,
            RecentMovements = recentMovements
        });
    }
}
