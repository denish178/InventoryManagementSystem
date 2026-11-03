using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.Suppliers;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SuppliersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Suppliers
    [HttpGet]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<IEnumerable<SupplierResponseDto>>> GetAll()
    {
        var suppliers = await _context.Suppliers
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SupplierResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Ok(suppliers);
    }

    // GET: api/Suppliers/2
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Inventory Staff,Viewer")]
    public async Task<ActionResult<SupplierResponseDto>> GetById(int id)
    {
        var supplier = await _context.Suppliers
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SupplierResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (supplier is null)
            return NotFound();

        return Ok(supplier);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<Supplier>> Create(
        CreateSupplierDto dto)
    {
        var supplier = new Supplier
        {
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            Address = dto.Address?.Trim()
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();


        var response = new SupplierResponseDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            IsActive = supplier.IsActive,
            CreatedAt = supplier.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, response);

    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateSupplierDto dto)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier is null)
            return NotFound();

        supplier.Name = dto.Name.Trim();
        supplier.ContactPerson = dto.ContactPerson?.Trim();
        supplier.Email = dto.Email?.Trim();
        supplier.Phone = dto.Phone?.Trim();
        supplier.Address = dto.Address?.Trim();
        supplier.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);

        if (supplier is null)
            return NotFound();

        var hasProducts = await _context.Products
            .AnyAsync(p => p.SupplierId == id);

        if (hasProducts)
        {
            return Conflict(new
            {
                message = "Supplier cannot be deleted because products use it."
            });
        }

        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}