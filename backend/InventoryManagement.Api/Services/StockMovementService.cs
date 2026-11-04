using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.StockTransactions;
using InventoryManagement.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Api.Services;

public enum StockMovementCode
{
    Success,
    NotFound,
    Inactive,
    InsufficientStock,
    InvalidQuantity,
    InvalidType,
    Overflow
}

public sealed record StockMovementResult(
    StockMovementCode Code,
    string? Message,
    StockTransactionResponseDto? Transaction)
{
    public static StockMovementResult Ok(StockTransactionResponseDto transaction) =>
        new(StockMovementCode.Success, null, transaction);

    public static StockMovementResult Fail(StockMovementCode code, string message) =>
        new(code, message, null);
}

public interface IStockMovementService
{
    Task<StockMovementResult> StockInAsync(
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken);

    Task<StockMovementResult> StockOutAsync(
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken);

    Task<StockMovementResult> CreateAsync(
        CreateStockTransactionDto request,
        string? createdBy,
        CancellationToken cancellationToken);
}

public class StockMovementService : IStockMovementService
{
    public const string StockIn = "StockIn";
    public const string StockOut = "StockOut";

    private readonly ApplicationDbContext _db;
    private readonly ILogger<StockMovementService> _logger;

    public StockMovementService(ApplicationDbContext db, ILogger<StockMovementService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task<StockMovementResult> StockInAsync(
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken) =>
        MoveAsync(StockIn, request, createdBy, cancellationToken);

    public Task<StockMovementResult> StockOutAsync(
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken) =>
        MoveAsync(StockOut, request, createdBy, cancellationToken);

    public Task<StockMovementResult> CreateAsync(
        CreateStockTransactionDto request,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        if (request.TransactionType is not (StockIn or StockOut))
        {
            return Task.FromResult(StockMovementResult.Fail(
                StockMovementCode.InvalidType,
                "TransactionType must be StockIn or StockOut."));
        }

        var movement = new StockMovementRequestDto
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes
        };

        return MoveAsync(request.TransactionType, movement, createdBy, cancellationToken);
    }

    private async Task<StockMovementResult> MoveAsync(
        string transactionType,
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return StockMovementResult.Fail(
                StockMovementCode.InvalidQuantity,
                "Quantity must be a whole number greater than zero.");
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await ApplyMovementAsync(
                    transactionType, request, createdBy, cancellationToken);

                if (result.Code != StockMovementCode.Success)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return result;
                }

                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock movement rolled back for product {ProductId}", request.ProductId);
                try
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(
                        rollbackEx,
                        "Stock movement rollback failed for product {ProductId}",
                        request.ProductId);
                }

                _db.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private async Task<StockMovementResult> ApplyMovementAsync(
        string transactionType,
        StockMovementRequestDto request,
        string? createdBy,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == request.ProductId)
            .Select(p => new { p.Id, p.Name, p.SKU, p.IsActive })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return StockMovementResult.Fail(StockMovementCode.NotFound, "Product not found.");
        }

        if (!product.IsActive)
        {
            return StockMovementResult.Fail(
                StockMovementCode.Inactive,
                "Cannot record stock for an inactive product.");
        }

        var now = DateTime.UtcNow;
        int affected;

        if (transactionType == StockIn)
        {
            var maxBefore = int.MaxValue - request.Quantity;
            affected = await _db.Products
                .Where(p => p.Id == product.Id && p.IsActive && p.QuantityInStock <= maxBefore)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.QuantityInStock, p => p.QuantityInStock + request.Quantity)
                    .SetProperty(p => p.UpdatedAt, now), cancellationToken);

            if (affected == 0)
            {
                return StockMovementResult.Fail(
                    StockMovementCode.Overflow,
                    "Stock quantity exceeds the supported limit.");
            }
        }
        else if (transactionType == StockOut)
        {
            affected = await _db.Products
                .Where(p => p.Id == product.Id && p.IsActive && p.QuantityInStock >= request.Quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.QuantityInStock, p => p.QuantityInStock - request.Quantity)
                    .SetProperty(p => p.UpdatedAt, now), cancellationToken);

            if (affected == 0)
            {
                return StockMovementResult.Fail(
                    StockMovementCode.InsufficientStock,
                    "Insufficient stock available.");
            }
        }
        else
        {
            return StockMovementResult.Fail(
                StockMovementCode.InvalidType,
                "TransactionType must be StockIn or StockOut.");
        }

        var movement = new StockTransaction
        {
            ProductId = product.Id,
            TransactionType = transactionType,
            Quantity = request.Quantity,
            ReferenceNumber = Clean(request.ReferenceNumber),
            Notes = Clean(request.Notes),
            CreatedAt = now,
            CreatedBy = createdBy
        };

        _db.StockTransactions.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);

        return StockMovementResult.Ok(new StockTransactionResponseDto
        {
            Id = movement.Id,
            ProductId = product.Id,
            ProductName = product.Name,
            SKU = product.SKU,
            TransactionType = movement.TransactionType,
            Quantity = movement.Quantity,
            ReferenceNumber = movement.ReferenceNumber,
            Notes = movement.Notes,
            CreatedAt = movement.CreatedAt,
            CreatedBy = movement.CreatedBy
        });
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
