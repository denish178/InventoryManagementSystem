using InventoryManagement.Api.DTOs.Users;
using InventoryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IUserAccountService _users;

    public UsersController(IUserAccountService users)
    {
        _users = users;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetUsers()
    {
        var users = await _users.ListAsync();
        return Ok(users);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponseDto>> CreateUser([FromBody] CreateUserDto request)
    {
        var (user, error) = await _users.CreateAsync(request);
        if (error is not null)
            return Status(error);

        return CreatedAtAction(nameof(GetUsers), new { id = user!.Id }, user);
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UpdateUserRoleDto request)
    {
        var error = await _users.ChangeRoleAsync(id, request.Role);
        if (error is not null)
            return Status(error);

        return Ok(new { message = "User role updated successfully.", userId = id, role = request.Role.Trim() });
    }

    private ObjectResult Status(IActionError error) =>
        StatusCode(error.StatusCode, new { message = error.Message, errors = error.Errors });
}
