using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Api.DTOs.Users;

public class UpdateUserRoleDto
{
    [Required]
    public string Role { get; set; } = string.Empty;
}