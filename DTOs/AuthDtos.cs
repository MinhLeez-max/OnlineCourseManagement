using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class RegisterDto
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, MinLength(8)]
        public string Password { get; set; } = "";
    }

    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}