
using InventoryManagement.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Inventory Staff,Viewer")]
public class ReportsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/reports/low-stock
    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                        p.QuantityInStock <= p.ReorderLevel)
            .OrderBy(p => p.QuantityInStock)
            .Select(p => new
            {
                p.Id,
                p.SKU,
                p.Name,
                p.Description,
                p.UnitPrice,
                p.QuantityInStock,
                p.ReorderLevel,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.SupplierId,
                SupplierName = p.Supplier.Name,
                StockStatus = "Low Stock"
            })
            .ToListAsync();

        return Ok(new
        {
            totalLowStockProducts = products.Count,
            products
        });
    }

    // GET: api/reports/inventory
    // Optional filters: categoryId, supplierId, search
    [HttpGet("inventory")]
    public async Task<IActionResult> GetInventoryReport(
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] string? search)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive);

        // Category filter
        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        // Supplier filter
        if (supplierId.HasValue)
        {
            query = query.Where(p =>
                p.SupplierId == supplierId.Value);
        }

        // Product name / SKU search
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.SKU.Contains(term));
        }

        // Summary calculations
        var totalProducts = await query.CountAsync();

        var totalUnits = await query
            .SumAsync(p => (long?)p.QuantityInStock) ?? 0;

        var inventoryValue = await query
            .SumAsync(p =>
                (decimal?)(p.UnitPrice * p.QuantityInStock)) ?? 0;

        // Inventory product details
        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.SKU,
                p.Name,
                p.UnitPrice,
                p.QuantityInStock,
                p.ReorderLevel,
                p.CategoryId,
                CategoryName = p.Category.Name,
                p.SupplierId,
                SupplierName = p.Supplier.Name,
                IsLowStock = p.QuantityInStock <= p.ReorderLevel,
                StockValue = p.UnitPrice * p.QuantityInStock
            })
            .ToListAsync();

        return Ok(new
        {
            totalProducts,
            totalUnits,
            inventoryValue,
            products
        });
    }


    // GET: api/reports/stock-movements
    [HttpGet("stock-movements")]
    public async Task<IActionResult> GetStockMovementReport(
        [FromQuery] int? productId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.StockTransactions
            .AsNoTracking()
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            var type = transactionType.Trim();

            if (type != "StockIn" && type != "StockOut")
            {
                return BadRequest(new
                {
                    message = "Transaction type must be StockIn or StockOut."
                });
            }

            query = query.Where(t => t.TransactionType == type);
        }

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(
                startDate.Value.Date, DateTimeKind.Utc);

            query = query.Where(t => t.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = DateTime.SpecifyKind(
                endDate.Value.Date.AddDays(1), DateTimeKind.Utc);

            query = query.Where(t => t.CreatedAt < endExclusive);
        }

        var totalTransactions = await query.CountAsync();

        var totalStockIn = await query
            .Where(t => t.TransactionType == "StockIn")
            .SumAsync(t => (long?)t.Quantity) ?? 0;

        var totalStockOut = await query
            .Where(t => t.TransactionType == "StockOut")
            .SumAsync(t => (long?)t.Quantity) ?? 0;

        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.ProductId,
                ProductName = t.Product.Name,
                SKU = t.Product.SKU,
                t.TransactionType,
                t.Quantity,
                t.ReferenceNumber,
                t.Notes,
                t.CreatedAt,
                t.CreatedBy
            })
            .ToListAsync();

        return Ok(new
        {
            totalTransactions,
            totalStockIn,
            totalStockOut,
            transactions
        });
    }

}
