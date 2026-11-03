
using InventoryManagement.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

        return Ok(new
        {
            TotalProducts = totalProducts,
            LowStockProducts = lowStockProducts,
            TotalUnitsInStock = totalUnitsInStock,
            InventoryValue = inventoryValue
        });
    }
}
