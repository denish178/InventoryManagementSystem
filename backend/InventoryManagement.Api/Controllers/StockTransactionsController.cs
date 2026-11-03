using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.StockTransactions;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StockTransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public StockTransactionsController(ApplicationDbContext context)
    {
        _context = context;
    }


    // GET: api/StockTransactions
    // Supports product, transaction type and date range filters
    [HttpGet]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<IEnumerable<StockTransactionResponseDto>>> GetAll(
        [FromQuery] int? productId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var query = _context.StockTransactions
            .AsNoTracking()
            .AsQueryable();

        // Filter by product
        if (productId.HasValue)
        {
            query = query.Where(t =>
                t.ProductId == productId.Value);
        }

        // Filter by transaction type
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

            query = query.Where(t =>
                t.TransactionType == type);
        }

        // Filter by start date (inclusive)
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(
                startDate.Value.Date,
                DateTimeKind.Utc);

            query = query.Where(t => t.CreatedAt >= start);
        }

        // Filter by end date (inclusive)
        if (endDate.HasValue)
        {
            var endExclusive = DateTime.SpecifyKind(
                endDate.Value.Date.AddDays(1),
                DateTimeKind.Utc);

            query = query.Where(t => t.CreatedAt < endExclusive);
        }

        // Newest transactions first
        var transactions = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new StockTransactionResponseDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product.Name,
                SKU = t.Product.SKU,
                TransactionType = t.TransactionType,
                Quantity = t.Quantity,
                ReferenceNumber = t.ReferenceNumber,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                CreatedBy = t.CreatedBy
            })
            .ToListAsync();

        return Ok(transactions);
    }


    // GET: api/StockTransactions/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<StockTransactionResponseDto>> GetById(int id)
    {
        var transaction = await _context.StockTransactions
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new StockTransactionResponseDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductName = t.Product.Name,
                SKU = t.Product.SKU,
                TransactionType = t.TransactionType,
                Quantity = t.Quantity,
                ReferenceNumber = t.ReferenceNumber,
                Notes = t.Notes,
                CreatedAt = t.CreatedAt,
                CreatedBy = t.CreatedBy
            })
            .FirstOrDefaultAsync();

        if (transaction is null)
            return NotFound();

        return Ok(transaction);
    }

    // POST: api/StockTransactions
    [HttpPost]
    [Authorize(Roles = "Admin,Inventory Staff")]
    public async Task<ActionResult<StockTransactionResponseDto>> Create(
        CreateStockTransactionDto dto)
    {
        await using var dbTransaction =
            await _context.Database.BeginTransactionAsync();

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == dto.ProductId);

        if (product is null)
            return NotFound(new { message = "Product not found." });

        if (!product.IsActive)
            return BadRequest(new { message = "Cannot transact stock for an inactive product." });

        if (dto.TransactionType == "StockIn")
        {
            if (product.QuantityInStock > int.MaxValue - dto.Quantity)
            {
                return BadRequest(new { message = "Stock quantity exceeds the supported limit." });
            }

            product.QuantityInStock += dto.Quantity;
        }
        else if (dto.TransactionType == "StockOut")
        {
            if (product.QuantityInStock < dto.Quantity)
            {
                return BadRequest(new { message = "Insufficient stock available." });
            }

            product.QuantityInStock -= dto.Quantity;
        }
        else
        {
            return BadRequest(new { message = "TransactionType must be StockIn or StockOut." });
        }

        product.UpdatedAt = DateTime.UtcNow;

        var transaction = new StockTransaction
        {
            ProductId = product.Id,
            TransactionType = dto.TransactionType,
            Quantity = dto.Quantity,
            ReferenceNumber = dto.ReferenceNumber?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.FindFirst(
        System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        };

        _context.StockTransactions.Add(transaction);

        await _context.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        var response = new StockTransactionResponseDto
        {
            Id = transaction.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            TransactionType = transaction.TransactionType,
            Quantity = transaction.Quantity,
            ReferenceNumber = transaction.ReferenceNumber,
            Notes = transaction.Notes,
            CreatedAt = transaction.CreatedAt,
            CreatedBy = transaction.CreatedBy
        };

        return CreatedAtAction(
            nameof(GetById),
            new { id = transaction.Id },
            response);
    }
}
