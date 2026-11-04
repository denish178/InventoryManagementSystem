using InventoryManagement.Api.DTOs.Users;
using InventoryManagement.Api.Entities;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Api.Services;

public interface IUserAccountService
{
    Task<IReadOnlyList<UserResponseDto>> ListAsync();
    Task<(UserResponseDto? User, IActionError? Error)> CreateAsync(CreateUserDto request);
    Task<IActionError?> ChangeRoleAsync(string userId, string role);
}

public sealed record IActionError(int StatusCode, string Message, IEnumerable<string>? Errors = null);

public class UserAccountService : IUserAccountService
{
    private static readonly string[] AllowedRoles = ["Admin", "Inventory Staff", "Viewer"];

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserAccountService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<UserResponseDto>> ListAsync()
    {
        var users = _userManager.Users.OrderByDescending(u => u.CreatedAt).ToList();
        var response = new List<UserResponseDto>();

        foreach (var user in users)
        {
            response.Add(await MapAsync(user));
        }

        return response;
    }

    public async Task<(UserResponseDto? User, IActionError? Error)> CreateAsync(CreateUserDto request)
    {
        var email = request.Email.Trim();
        var role = request.Role.Trim();

        if (!AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            return (null, new IActionError(400, "Invalid role. Allowed roles: Admin, Inventory Staff, Viewer."));
        }

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return (null, new IActionError(409, "A user with this email already exists."));
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return (null, new IActionError(400, "The selected role does not exist."));
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return (null, new IActionError(
                400,
                "User creation failed.",
                createResult.Errors.Select(e => e.Description)));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return (null, new IActionError(
                400,
                "User role assignment failed.",
                roleResult.Errors.Select(e => e.Description)));
        }

        return (await MapAsync(user), null);
    }

    public async Task<IActionError?> ChangeRoleAsync(string userId, string role)
    {
        role = role.Trim();

        if (!AllowedRoles.Contains(role, StringComparer.Ordinal))
        {
            return new IActionError(400, "Invalid role. Allowed roles: Admin, Inventory Staff, Viewer.");
        }

        if (!await _roleManager.RoleExistsAsync(role))
        {
            return new IActionError(400, "The selected role does not exist.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new IActionError(404, "User not found.");
        }

        var existingRoles = await _userManager.GetRolesAsync(user);
        if (existingRoles.Contains(role) && existingRoles.Count == 1)
        {
            return null;
        }

        if (existingRoles.Contains("Admin") && role != "Admin")
        {
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            if (user.IsActive && admins.Count(u => u.IsActive) <= 1)
            {
                return new IActionError(400, "Cannot remove the last active Admin.");
            }
        }

        var removeResult = await _userManager.RemoveFromRolesAsync(user, existingRoles);
        if (!removeResult.Succeeded)
        {
            return new IActionError(400, "Existing role removal failed.", removeResult.Errors.Select(e => e.Description));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
        {
            if (existingRoles.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, existingRoles);
            }

            return new IActionError(400, "Role update failed.", addResult.Errors.Select(e => e.Description));
        }

        return null;
    }

    private async Task<UserResponseDto> MapAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Roles = roles
        };
    }
}
