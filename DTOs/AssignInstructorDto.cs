using System.ComponentModel.DataAnnotations;

namespace OnlineCourseManagement.DTOs
{
    public class AssignInstructorDto
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int InstructorId { get; set; }
    }
}