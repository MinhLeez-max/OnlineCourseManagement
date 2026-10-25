using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = "";

        [Required]
        public string Role { get; set; } = "Student";
    }

    public class UpdateUserRoleDto
    {
        [Required]
        public string Role { get; set; } = "";
    }
}