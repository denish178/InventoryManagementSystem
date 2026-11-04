
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using InventoryManagement.Api.DTOs;
using InventoryManagement.Api.DTOs.Users;
using InventoryManagement.Api.Entities;
using InventoryManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IUserAccountService _users;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IUserAccountService users)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _users = users;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register([FromBody] CreateUserDto request)
    {
        var (user, error) = await _users.CreateAsync(request);
        if (error is not null)
        {
            return StatusCode(error.StatusCode, new { message = error.Message, errors = error.Errors });
        }

        return Created($"/api/users/{user!.Id}", user);
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!signInResult.Succeeded)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("full_name", user.FullName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim("role", role));
        }

        var keyText = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("JWT issuer is missing.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("JWT audience is missing.");

        var key = new SymmetricSecurityKey(
            Convert.FromBase64String(keyText));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Token = tokenString,
            ExpiresAt = expiresAt,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Roles = roles
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FullName,
            Roles = roles
        });
    }
}
