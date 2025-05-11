using System.ComponentModel.DataAnnotations;

namespace App1.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Храните хеш, а не пароль!

        [MaxLength(200)]
        public string? FullName { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}