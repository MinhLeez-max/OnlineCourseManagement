using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class CreateCourseDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        // Admin chỉ định; Instructor không được
        // tự gán khóa học cho tài khoản khác.
        public int? InstructorId { get; set; }
    }

    public class UpdateCourseDto : CreateCourseDto
    {
    }
}