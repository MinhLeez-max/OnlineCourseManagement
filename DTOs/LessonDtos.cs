using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class CreateLessonDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Content { get; set; }

        [MaxLength(1000)]
        public string? VideoUrl { get; set; }

        [Range(0, int.MaxValue)]
        public int DurationMinutes { get; set; }

        [Range(1, int.MaxValue)]
        public int OrderIndex { get; set; }

        [Range(1, int.MaxValue)]
        public int CourseId { get; set; }
    }

    public class UpdateLessonDto : CreateLessonDto
    {
    }
}