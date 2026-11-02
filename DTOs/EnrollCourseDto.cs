using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class EnrollCourseDto
    {
        [Range(1, int.MaxValue)]
        public int CourseId { get; set; }
    }
}