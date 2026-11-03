using InventoryManagement.Api.Data;
using InventoryManagement.Api.DTOs.Categories;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class CategoriesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CategoriesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Categories
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll()
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(categories);
    }

    // GET: api/Categories/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CategoryResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (category is null)
            return NotFound();

        return Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult<Category>> Create(
        CreateCategoryDto dto)
    {
        var name = dto.Name.Trim();

        var exists = await _context.Categories
            .AnyAsync(c => c.Name == name);

        if (exists)
            return Conflict(new { message = "Category already exists." });

        var category = new Category
        {
            Name = name,
            Description = dto.Description?.Trim()
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var response = new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = category.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        var name = dto.Name.Trim();

        var duplicate = await _context.Categories.AnyAsync(
            c => c.Id != id && c.Name == name);

        if (duplicate)
            return Conflict(new { message = "Category already exists." });

        category.Name = name;
        category.Description = dto.Description?.Trim();
        category.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category is null)
            return NotFound();

        var hasProducts = await _context.Products
            .AnyAsync(p => p.CategoryId == id);

        if (hasProducts)
        {
            return Conflict(new
            {
                message = "Category cannot be deleted because products use it."
            });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}