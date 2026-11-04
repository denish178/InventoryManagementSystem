using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.StockTransactions;
using InventoryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/stock-transactions")]
[Authorize]
public class StockTransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStockMovementService _stock;

    public StockTransactionsController(ApplicationDbContext context, IStockMovementService stock)
    {
        _context = context;
        _stock = stock;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<IEnumerable<StockTransactionResponseDto>>> GetAll(
        [FromQuery] int? productId,
        [FromQuery] string? transactionType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var query = _context.StockTransactions.AsNoTracking().AsQueryable();

        if (productId.HasValue)
            query = query.Where(t => t.ProductId == productId.Value);

        if (!string.IsNullOrWhiteSpace(transactionType))
        {
            var type = transactionType.Trim();
            if (type != StockMovementService.StockIn && type != StockMovementService.StockOut)
            {
                return BadRequest(new { message = "Transaction type must be StockIn or StockOut." });
            }

            query = query.Where(t => t.TransactionType == type);
        }

        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt >= start);
        }

        if (endDate.HasValue)
        {
            var endExclusive = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(t => t.CreatedAt < endExclusive);
        }

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
            .ToListAsync(cancellationToken);

        return Ok(transactions);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<StockTransactionResponseDto>> GetById(int id, CancellationToken cancellationToken)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (transaction is null)
            return NotFound();

        return Ok(transaction);
    }

    [HttpPost("stock-in")]
    [Authorize(Roles = "Admin,Inventory Staff")]
    public Task<ActionResult<StockTransactionResponseDto>> StockIn(
        StockMovementRequestDto request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _stock.StockInAsync(request, CurrentUser(), cancellationToken));

    [HttpPost("stock-out")]
    [Authorize(Roles = "Admin,Inventory Staff")]
    public Task<ActionResult<StockTransactionResponseDto>> StockOut(
        StockMovementRequestDto request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _stock.StockOutAsync(request, CurrentUser(), cancellationToken));

    [HttpPost]
    [Authorize(Roles = "Admin,Inventory Staff")]
    public Task<ActionResult<StockTransactionResponseDto>> Create(
        CreateStockTransactionDto request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(() => _stock.CreateAsync(request, CurrentUser(), cancellationToken));

    private async Task<ActionResult<StockTransactionResponseDto>> ExecuteAsync(
        Func<Task<StockMovementResult>> action)
    {
        var result = await action();
        return result.Code switch
        {
            StockMovementCode.Success => CreatedAtAction(
                nameof(GetById),
                new { id = result.Transaction!.Id },
                result.Transaction),
            StockMovementCode.NotFound => NotFound(new { message = result.Message }),
            StockMovementCode.InsufficientStock => Conflict(new { message = result.Message }),
            _ => BadRequest(new { message = result.Message })
        };
    }

    private string? CurrentUser() =>
        User.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User.FindFirstValue(ClaimTypes.Email)
        ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}
