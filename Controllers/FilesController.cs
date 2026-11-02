using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.Models;
using System.Security.Claims;

namespace OnlineCourseManagement.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly string _storageRoot;

    public FilesController(
        AppDbContext db,
        IWebHostEnvironment env)
    {
        _db = db;
        _storageRoot = Path.Combine(
            env.ContentRootPath, "Uploads");
    }

    private int? CurrentUserId()
    {
        var value = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        return int.TryParse(value, out var id)
            ? id : null;
    }

    private bool CanManage(Course course)
    {
        return User.IsInRole("Admin") ||
            (User.IsInRole("Instructor") &&
             CurrentUserId() != null &&
             course.InstructorId == CurrentUserId());
    }

    private static bool IsValidSignature(
        byte[] header, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" =>
                header.Length >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF,

            ".png" =>
                header.Length >= 8 &&
                header.Take(8).SequenceEqual(
                    new byte[]
                    {
                        137, 80, 78, 71, 13, 10, 26, 10
                    }),

            ".webp" =>
                header.Length >= 12 &&
                System.Text.Encoding.ASCII
                    .GetString(header, 0, 4) == "RIFF" &&
                System.Text.Encoding.ASCII
                    .GetString(header, 8, 4) == "WEBP",

            ".pdf" =>
                header.Length >= 5 &&
                System.Text.Encoding.ASCII
                    .GetString(header, 0, 5) == "%PDF-",

            _ => false
        };
    }

    // POST: api/files/courses/1/image
    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("courses/{courseId:int}/image")]
    [RequestSizeLimit(6_000_000)]
    public async Task<IActionResult> UploadCourseImage(
        int courseId,
        IFormFile file)
    {
        var course = await _db.Courses
            .FindAsync(courseId);

        if (course == null)
            return NotFound("Khóa học không tồn tại");

        if (!CanManage(course))
            return Forbid();

        return await SaveFile(
            file, "CourseImage", courseId, null);
    }

    // POST: api/files/lessons/1/document
    [Authorize(Roles = "Admin,Instructor")]
    [HttpPost("lessons/{lessonId:int}/document")]
    [RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> UploadLessonDocument(
        int lessonId,
        IFormFile file)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound("Bài học không tồn tại");

        if (lesson.Course == null ||
            !CanManage(lesson.Course))
            return Forbid();

        return await SaveFile(
            file, "LessonDocument",
            lesson.CourseId, lesson.Id);
    }

    private async Task<IActionResult> SaveFile(
        IFormFile file,
        string type,
        int courseId,
        int? lessonId)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Vui lòng chọn file");

        var maxSize = type == "CourseImage"
            ? 5_000_000L : 10_000_000L;

        if (file.Length > maxSize)
            return BadRequest("File vượt quá dung lượng");

        var extension = Path.GetExtension(
            file.FileName).ToLowerInvariant();

        string[] allowed = type == "CourseImage"
            ? [".jpg", ".jpeg", ".png", ".webp"]
            : [".pdf"];

        if (!allowed.Contains(extension))
            return BadRequest("Định dạng file không hợp lệ");

        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        await using var input = file.OpenReadStream();

        var header = new byte[12];
        var read = await input.ReadAsync(
            header.AsMemory());

        if (!IsValidSignature(
            header[..read], extension))
            return BadRequest("Nội dung file không hợp lệ");

        input.Position = 0;

        var directory = Path.Combine(
            _storageRoot,
            type == "CourseImage"
                ? "course-images"
                : "lesson-documents");

        Directory.CreateDirectory(directory);

        var storedName =
            $"{Guid.NewGuid():N}{extension}";

        var path = Path.Combine(
            directory, storedName);

        try
        {
            await using (var output = new FileStream(
                path, FileMode.CreateNew,
                FileAccess.Write, FileShare.None))
            {
                await input.CopyToAsync(output);
            }

            var record = new UploadedFile
            {
                OriginalName = Path.GetFileName(
                    file.FileName),
                StoredName = storedName,
                ContentType = contentType,
                Size = file.Length,
                FileType = type,
                CourseId = courseId,
                LessonId = lessonId,
                UploadedById = CurrentUserId()!.Value
            };

            _db.UploadedFiles.Add(record);
            await _db.SaveChangesAsync();

            return StatusCode(201, new
            {
                message = "Upload thành công",
                record.Id,
                record.OriginalName,
                record.Size,
                record.FileType
            });
        }
        catch
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            throw;
        }
    }

    // GET: api/files/1/download
    [Authorize]
    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await _db.UploadedFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);

        if (file == null)
            return NotFound();

        // Tài liệu cần quyền học hoặc quyền quản lý
        var course = await _db.Courses
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Id == file.CourseId);

        if (course == null)
            return NotFound();

        bool allowed = CanManage(course);

        if (!allowed && User.IsInRole("Student"))
        {
            var userId = CurrentUserId();

            allowed = userId.HasValue &&
                await _db.Enrollments.AnyAsync(e =>
                    e.StudentId == userId.Value &&
                    e.CourseId == course.Id);
        }

        if (!allowed)
            return Forbid();

        var folder = file.FileType switch
        {
            "CourseImage" => "course-images",
            "LessonDocument" => "lesson-documents",
            _ => null
        };

        if (folder == null)
            return NotFound();

        var path = Path.Combine(
            _storageRoot, folder, file.StoredName);

        if (!System.IO.File.Exists(path))
            return NotFound("File không tồn tại trên ổ đĩa");

        var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read,
            FileShare.Read);

        return File(
            stream,
            file.ContentType,
            file.OriginalName);
    }

    // GET: api/files/courses/1
    [AllowAnonymous]
    [HttpGet("courses/{courseId:int}")]
    public async Task<IActionResult> GetFiles(int courseId)
    {
        var exists = await _db.Courses
            .AnyAsync(c => c.Id == courseId);

        if (!exists)
            return NotFound();

        // Chỉ liệt kê ảnh bìa công khai
        var files = await _db.UploadedFiles
            .AsNoTracking()
            .Where(f =>
                f.CourseId == courseId &&
                f.FileType == "CourseImage")
            .Select(f => new
            {
                f.Id,
                f.OriginalName,
                f.Size,
                f.UploadedAt
            })
            .ToListAsync();

        return Ok(files);
    }

    // GET: api/files/lessons/1
    [Authorize]
    [HttpGet("lessons/{lessonId:int}")]
    public async Task<IActionResult> GetLessonFiles(
        int lessonId)
    {
        var lesson = await _db.Lessons
            .AsNoTracking()
            .Include(l => l.Course)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson == null)
            return NotFound();

        if (lesson.Course == null)
            return NotFound();

        bool allowed = CanManage(lesson.Course);

        if (!allowed && User.IsInRole("Student"))
        {
            var userId = CurrentUserId();

            allowed = userId.HasValue &&
                await _db.Enrollments.AnyAsync(e =>
                    e.StudentId == userId.Value &&
                    e.CourseId == lesson.CourseId);
        }

        if (!allowed)
            return Forbid();

        var files = await _db.UploadedFiles
            .AsNoTracking()
            .Where(f =>
                f.LessonId == lessonId &&
                f.FileType == "LessonDocument")
            .Select(f => new
            {
                f.Id,
                f.OriginalName,
                f.Size,
                f.UploadedAt
            })
            .ToListAsync();

        return Ok(files);
    }

    // DELETE: api/files/1
    [Authorize(Roles = "Admin,Instructor")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var file = await _db.UploadedFiles
            .FirstOrDefaultAsync(f => f.Id == id);

        if (file == null)
            return NotFound();

        var course = await _db.Courses
            .FirstOrDefaultAsync(c =>
                c.Id == file.CourseId);

        if (course == null)
            return NotFound();

        if (!CanManage(course))
            return Forbid();

        var folder = file.FileType switch
        {
            "CourseImage" => "course-images",
            "LessonDocument" => "lesson-documents",
            _ => null
        };

        if (folder == null)
            return BadRequest("Loại file không hợp lệ");

        var path = Path.Combine(
            _storageRoot, folder, file.StoredName);

        _db.UploadedFiles.Remove(file);
        await _db.SaveChangesAsync();

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        return Ok(new
        {
            message = "Xóa file thành công"
        });
    }
}