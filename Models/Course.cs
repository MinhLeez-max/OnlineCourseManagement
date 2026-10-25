using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OnlineCourseManagement.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        // Nullable để giữ khóa học cũ
        public int? InstructorId { get; set; }
        public User? Instructor { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}