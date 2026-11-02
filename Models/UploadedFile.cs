using System.ComponentModel.DataAnnotations;
namespace OnlineCourseManagement.Models;

public class UploadedFile
{
    public int Id { get; set; }

    public string OriginalName { get; set; } = "";

    public string StoredName { get; set; } = "";

    public string ContentType { get; set; } = "";

    public long Size { get; set; }

    public string FileType { get; set; } = "";

    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    public int? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    public int UploadedById { get; set; }

    public DateTime UploadedAt { get; set; }
        = DateTime.UtcNow;
}