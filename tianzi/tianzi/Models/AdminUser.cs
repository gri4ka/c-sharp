using System.ComponentModel.DataAnnotations;

namespace tianzi.Models;

public class AdminUser
{
    public int Id { get; set; }

    [Required, MaxLength(64)]
    public string Username { get; set; } = "";

    [Required]
    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
