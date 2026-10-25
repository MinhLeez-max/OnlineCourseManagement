using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}