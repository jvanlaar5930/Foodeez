namespace Foodeez.Application.DTOs.Admin;

public class AdminUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsActive { get; set; }
    public bool ProfileCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
}
