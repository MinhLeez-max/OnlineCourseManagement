using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.Models;
using System.Security.Claims;

namespace OnlineCourseManagement.Controllers
{
    [ApiController]
    [Route("api/progress")]
    [Authorize]
    public class LessonProgressController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LessonProgressController(AppDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out var id)
                ? id : null;
        }

        // POST: api/progress/lessons/1/complete
        [Authorize(Roles = "Student")]
        [HttpPost("lessons/{lessonId:int}/complete")]
        public async Task<IActionResult> CompleteLesson(
            int lessonId)
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                return NotFound(new
                {
                    message = "Bài học không tồn tại"
                });

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId.Value &&
                    e.CourseId == lesson.CourseId);

            if (enrollment == null)
                return Forbid();

            var progress = await _context.LessonProgresses
                .FirstOrDefaultAsync(p =>
                    p.EnrollmentId == enrollment.Id &&
                    p.LessonId == lessonId);

            if (progress == null)
            {
                progress = new LessonProgress
                {
                    EnrollmentId = enrollment.Id,
                    LessonId = lessonId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                };

                _context.LessonProgresses.Add(progress);
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }

            // Nếu đã hoàn thành thì không ghi lại thời gian
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Có thể có yêu cầu đồng thời hoặc
                // dữ liệu liên quan vừa thay đổi
                return Conflict(new
                {
                    message = "Không thể cập nhật tiến độ. Hãy tải lại."
                });
            }

            return Ok(new
            {
                message = "Đã hoàn thành bài học",
                progress.LessonId,
                progress.IsCompleted,
                progress.CompletedAt
            });
        }

        // DELETE: api/progress/lessons/1/complete
        [Authorize(Roles = "Student")]
        [HttpDelete("lessons/{lessonId:int}/complete")]
        public async Task<IActionResult> UndoComplete(
            int lessonId)
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var progress = await _context.LessonProgresses
                .Include(p => p.Enrollment)
                .FirstOrDefaultAsync(p =>
                    p.LessonId == lessonId &&
                    p.Enrollment.StudentId == studentId.Value &&
                    p.Enrollment.CourseId == p.Lesson.CourseId);

            if (progress == null)
                return NotFound(new
                {
                    message = "Chưa có tiến độ cho bài học này"
                });

            progress.IsCompleted = false;
            progress.CompletedAt = null;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Đã hủy trạng thái hoàn thành",
                progress.LessonId,
                progress.IsCompleted
            });
        }

        // GET: api/progress/courses/1
        [Authorize(Roles = "Student")]
        [HttpGet("courses/{courseId:int}")]
        public async Task<IActionResult> GetCourseProgress(
            int courseId)
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId.Value &&
                    e.CourseId == courseId);

            if (enrollment == null)
                return Forbid();

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound();

            var lessons = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.OrderIndex
                })
                .ToListAsync();

            var completed = await _context.LessonProgresses
                .AsNoTracking()
                .Where(p =>
                    p.EnrollmentId == enrollment.Id &&
                    p.IsCompleted &&
                    p.Lesson.CourseId == courseId)
                .Select(p => p.LessonId)
                .ToListAsync();

            var completedSet = completed.ToHashSet();

            var totalLessons = lessons.Count;
            var completedLessons = lessons.Count(l =>
                completedSet.Contains(l.Id));

            var percentage = totalLessons == 0
                ? 0
                : Math.Round(
                    completedLessons * 100.0 / totalLessons, 2);

            return Ok(new
            {
                CourseId = course.Id,
                CourseName = course.Title,
                TotalLessons = totalLessons,
                CompletedLessons = completedLessons,
                Percentage = percentage,
                Lessons = lessons.Select(l => new
                {
                    LessonId = l.Id,
                    l.Title,
                    l.OrderIndex,
                    IsCompleted = completedSet.Contains(l.Id)
                })
            });
        }

        // GET: api/progress/my
        [Authorize(Roles = "Student")]
        [HttpGet("my")]
        public async Task<IActionResult> MyProgress()
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var data = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId.Value)
                .Select(e => new
                {
                    e.CourseId,
                    CourseName = e.Course.Title,

                    TotalLessons = _context.Lessons
                        .Count(l => l.CourseId == e.CourseId),

                    CompletedLessons = _context.LessonProgresses
                        .Count(p =>
                            p.EnrollmentId == e.Id &&
                            p.IsCompleted &&
                            p.Lesson.CourseId == e.CourseId)
                })
                .ToListAsync();

            var result = data.Select(x => new
            {
                x.CourseId,
                x.CourseName,
                x.TotalLessons,
                x.CompletedLessons,
                Percentage = x.TotalLessons == 0
                    ? 0
                    : Math.Round(
                        x.CompletedLessons * 100.0 /
                        x.TotalLessons, 2)
            });

            return Ok(result);
        }

        // GET: api/progress/courses/1/students
        [Authorize(Roles = "Admin,Instructor")]
        [HttpGet("courses/{courseId:int}/students")]
        public async Task<IActionResult> GetStudentsProgress(
            int courseId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound();

            if (!User.IsInRole("Admin") &&
                course.InstructorId != CurrentUserId())
                return Forbid();

            var totalLessons = await _context.Lessons
                .CountAsync(l => l.CourseId == courseId);

            var data = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e => new
                {
                    e.StudentId,
                    StudentName = e.Student.FullName,
                    StudentEmail = e.Student.Email,
                    e.EnrolledAt,

                    CompletedLessons = _context.LessonProgresses
                        .Count(p =>
                            p.EnrollmentId == e.Id &&
                            p.IsCompleted &&
                            p.Lesson.CourseId == courseId)
                })
                .ToListAsync();

            var students = data.Select(s => new
            {
                s.StudentId,
                s.StudentName,
                s.StudentEmail,
                s.EnrolledAt,
                TotalLessons = totalLessons,
                s.CompletedLessons,
                Percentage = totalLessons == 0
                    ? 0
                    : Math.Round(
                        s.CompletedLessons * 100.0 /
                        totalLessons, 2)
            });

            return Ok(new
            {
                CourseId = courseId,
                CourseName = course.Title,
                TotalStudents = data.Count,
                Students = students
            });
        }
    }
}