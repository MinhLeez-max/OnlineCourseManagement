using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.Models
{
    public class Lesson
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [MaxLength(1000)]
        public string? VideoUrl { get; set; }

        public int DurationMinutes { get; set; }

        public int OrderIndex { get; set; }

        public int CourseId { get; set; }

        public Course? Course { get; set; }

        public DateTime CreatedAt { get; set; }
            = DateTime.UtcNow;
    }
}