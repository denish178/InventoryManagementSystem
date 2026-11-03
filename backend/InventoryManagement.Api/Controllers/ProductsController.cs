using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.Products;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }


    // GET: api/Products
    // Supports search, category, supplier and low-stock filters
    [HttpGet]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? supplierId,
        [FromQuery] bool? lowStockOnly)
    {
        var query = _context.Products
            .AsNoTracking()
            .AsQueryable();

        // Search by product name or SKU
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(p =>
                p.Name.Contains(term) ||
                p.SKU.Contains(term));
        }

        // Filter by category
        if (categoryId.HasValue)
        {
            query = query.Where(p =>
                p.CategoryId == categoryId.Value);
        }

        // Filter by supplier
        if (supplierId.HasValue)
        {
            query = query.Where(p =>
                p.SupplierId == supplierId.Value);
        }

        // Filter low-stock products
        if (lowStockOnly == true)
        {
            query = query.Where(p =>
                p.IsActive &&
                p.QuantityInStock <= p.ReorderLevel);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }


    // GET: api/Products/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (product is null)
            return NotFound();

        return Ok(product);
    }

    // GET: api/Products/low-stock
    [HttpGet("low-stock")]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<IEnumerable<ProductResponseDto>>> GetLowStock()
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive &&
                        p.QuantityInStock <= p.ReorderLevel)
            .OrderBy(p => p.QuantityInStock)
            .Select(p => new ProductResponseDto
            {
                Id = p.Id,
                SKU = p.SKU,
                Name = p.Name,
                Description = p.Description,
                UnitPrice = p.UnitPrice,
                QuantityInStock = p.QuantityInStock,
                ReorderLevel = p.ReorderLevel,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return Ok(products);
    }

    // POST: api/Products
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Product>> Create(
        CreateProductDto dto)
    {
        var sku = dto.SKU.Trim();

        var skuExists = await _context.Products
            .AnyAsync(p => p.SKU == sku);

        if (skuExists)
        {
            return Conflict(new
            {
                message = "A product with this SKU already exists."
            });
        }

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist or is inactive."
            });
        }

        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.Id == dto.SupplierId && s.IsActive);

        if (!supplierExists)
        {
            return BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            });
        }

        var product = new Product
        {
            SKU = sku,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            UnitPrice = dto.UnitPrice,
            QuantityInStock = 0,
            ReorderLevel = dto.ReorderLevel,
            CategoryId = dto.CategoryId,
            SupplierId = dto.SupplierId
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var response = new ProductResponseDto
        {
            Id = product.Id,
            SKU = product.SKU,
            Name = product.Name,
            Description = product.Description,
            UnitPrice = product.UnitPrice,
            QuantityInStock = product.QuantityInStock,
            ReorderLevel = product.ReorderLevel,
            CategoryId = product.CategoryId,
            CategoryName = await _context.Categories
        .Where(c => c.Id == product.CategoryId)
        .Select(c => c.Name)
        .SingleAsync(),
            SupplierId = product.SupplierId,
            SupplierName = await _context.Suppliers
        .Where(s => s.Id == product.SupplierId)
        .Select(s => s.Name)
        .SingleAsync(),
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    // PUT: api/Products/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateProductDto dto)
    {
        var product = await _context.Products.FindAsync(id);

        if (product is null)
            return NotFound();

        var categoryExists = await _context.Categories
            .AnyAsync(c => c.Id == dto.CategoryId && c.IsActive);

        if (!categoryExists)
        {
            return BadRequest(new
            {
                message = "The selected category does not exist or is inactive."
            });
        }

        var supplierExists = await _context.Suppliers
            .AnyAsync(s => s.Id == dto.SupplierId && s.IsActive);

        if (!supplierExists)
        {
            return BadRequest(new
            {
                message = "The selected supplier does not exist or is inactive."
            });
        }

        product.Name = dto.Name.Trim();
        product.Description = dto.Description?.Trim();
        product.UnitPrice = dto.UnitPrice;
        product.ReorderLevel = dto.ReorderLevel;
        product.CategoryId = dto.CategoryId;
        product.SupplierId = dto.SupplierId;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Products/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null)
            return NotFound();

        var hasTransactions = await _context.StockTransactions
            .AnyAsync(t => t.ProductId == id);

        if (hasTransactions)
        {
            return Conflict(new
            {
                message = "This product has stock transaction history and cannot be deleted."
            });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}