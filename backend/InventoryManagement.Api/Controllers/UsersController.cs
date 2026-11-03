
using InventoryManagement.Api.DTOs.Users;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private static readonly string[] AllowedRoles =
    {
        "Admin",
        "Inventory Staff",
        "Viewer"
    };

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: api/Users
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToList();

        var response = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            response.Add(new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                Roles = roles
            });
        }

        return Ok(response);
    }

    // POST: api/Users
    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser(
        [FromBody] CreateUserDto request)
    {
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();
        var role = request.Role.Trim();

        if (!AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                message = "Invalid role. Allowed roles: Admin, Inventory Staff, Viewer."
            });
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return Conflict(new
            {
                message = "A user with this email already exists."
            });
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new
            {
                message = "The selected role does not exist."
            });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(
            user,
            request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "User creation failed.",
                errors = createResult.Errors.Select(e => e.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return BadRequest(new
            {
                message = "User role assignment failed.",
                errors = roleResult.Errors.Select(e => e.Description)
            });
        }

        var response = new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = new List<string> { role }
        };

        return CreatedAtAction(
            nameof(GetUsers),
            new { id = user.Id },
            response);
    }

    // PUT: api/Users/{id}/role
    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(
        string id,
        [FromBody] UpdateUserRoleDto request)
    {
        var role = request.Role.Trim();

        if (!AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            return BadRequest(new
            {
                message = "Invalid role. Allowed roles: Admin, Inventory Staff, Viewer."
            });
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new
            {
                message = "The selected role does not exist."
            });
        }

        var user = await _userManager.FindByIdAsync(id);

        if (user is null)
        {
            return NotFound(new
            {
                message = "User not found."
            });
        }

        var existingRoles = await _userManager.GetRolesAsync(user);

        if (existingRoles.Contains(role))
        {
            return Ok(new
            {
                message = "User already has the selected role."
            });
        }

        // Prevent removing the last active Admin.
        if (existingRoles.Contains("Admin") && role != "Admin")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");

            var activeAdminCount = admins.Count(u => u.IsActive);

            if (user.IsActive && activeAdminCount <= 1)
            {
                return BadRequest(new
                {
                    message = "Cannot remove the last active Admin."
                });
            }
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(
            user,
            existingRoles);

        if (!removeResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Existing role removal failed.",
                errors = removeResult.Errors.Select(e => e.Description)
            });
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);

        if (!addResult.Succeeded)
        {
            // Restore previous roles if new role assignment fails.
            if (existingRoles.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, existingRoles);
            }

            return BadRequest(new
            {
                message = "Role update failed.",
                errors = addResult.Errors.Select(e => e.Description)
            });
        }

        return Ok(new
        {
            message = "User role updated successfully.",
            userId = user.Id,
            role
        });
    }
}
