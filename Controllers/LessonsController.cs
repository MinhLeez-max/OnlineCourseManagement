using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.DTOs;
using OnlineCourseManagement.Models;
using System.Security.Claims;

namespace OnlineCourseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LessonsController(AppDbContext context)
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

        private bool CanManage(Course course)
        {
            return User.IsInRole("Admin") ||
                (
                    User.IsInRole("Instructor") &&
                    CurrentUserId() != null &&
                    course.InstructorId == CurrentUserId()
                );
        }

        // GET: api/lessons
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var lessons = await _context.Lessons
                .AsNoTracking()
                .OrderBy(l => l.CourseId)
                .ThenBy(l => l.OrderIndex)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.DurationMinutes,
                    l.OrderIndex,
                    l.CourseId
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // GET: api/lessons/{id}
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound(new
                {
                    message = "Bài học không tồn tại"
                });

            // Admin được xem tất cả
            if (User.IsInRole("Admin"))
                return Ok(lesson);

            var userId = CurrentUserId();

            if (userId == null)
                return Unauthorized();

            // Giảng viên xem bài học của chính mình
            if (User.IsInRole("Instructor"))
            {
                var isOwner = await _context.Courses
                    .AnyAsync(c =>
                        c.Id == lesson.CourseId &&
                        c.InstructorId == userId.Value);

                if (!isOwner)
                    return Forbid();

                return Ok(lesson);
            }

            // Student phải đăng ký khóa học
            if (User.IsInRole("Student"))
            {
                var enrolled = await _context.Enrollments
                    .AnyAsync(e =>
                        e.StudentId == userId.Value &&
                        e.CourseId == lesson.CourseId);

                if (!enrolled)
                    return Forbid();

                return Ok(lesson);
            }

            return Forbid();
        }

        // GET: api/lessons/course/{courseId}
        [HttpGet("course/{courseId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByCourse(
    int courseId)
        {
            var exists = await _context.Courses
                .AnyAsync(c => c.Id == courseId);

            if (!exists)
                return NotFound(new
                {
                    message = "Khóa học không tồn tại"
                });

            var lessons = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.DurationMinutes,
                    l.OrderIndex,
                    l.CourseId
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // POST: api/lessons
        [HttpPost]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Create(
            [FromBody] CreateLessonDto dto)
        {
            var course = await _context.Courses
                .FindAsync(dto.CourseId);

            if (course == null)
                return BadRequest(new
                {
                    message = "Khóa học không tồn tại"
                });

            if (!CanManage(course))
                return Forbid();

            var orderExists = await _context.Lessons
                .AnyAsync(l =>
                    l.CourseId == dto.CourseId &&
                    l.OrderIndex == dto.OrderIndex);

            if (orderExists)
                return Conflict(new
                {
                    message = "Thứ tự bài học đã tồn tại"
                });

            var lesson = new Lesson
            {
                Title = dto.Title,
                Content = dto.Content,
                VideoUrl = dto.VideoUrl,
                DurationMinutes = dto.DurationMinutes,
                OrderIndex = dto.OrderIndex,
                CourseId = dto.CourseId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = lesson.Id },
                new
                {
                    message = "Thêm bài học thành công",
                    lesson.Id,
                    lesson.Title
                });
        }

        // PUT: api/lessons/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLessonDto dto)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            if (lesson.Course == null ||
                !CanManage(lesson.Course))
                return Forbid();

            var targetCourse = await _context.Courses
                .FindAsync(dto.CourseId);

            if (targetCourse == null)
                return BadRequest(new
                {
                    message = "Khóa học đích không tồn tại"
                });

            // Kiểm tra cả khóa học đích,
            // tránh chuyển bài sang khóa học người khác.
            if (!CanManage(targetCourse))
                return Forbid();

            var orderExists = await _context.Lessons
                .AnyAsync(l =>
                    l.Id != id &&
                    l.CourseId == dto.CourseId &&
                    l.OrderIndex == dto.OrderIndex);

            if (orderExists)
                return Conflict(new
                {
                    message = "Thứ tự bài học đã tồn tại"
                });

            lesson.Title = dto.Title;
            lesson.Content = dto.Content;
            lesson.VideoUrl = dto.VideoUrl;
            lesson.DurationMinutes = dto.DurationMinutes;
            lesson.OrderIndex = dto.OrderIndex;
            lesson.CourseId = dto.CourseId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật bài học thành công"
            });
        }

        // DELETE: api/lessons/{id}
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Instructor")]
        public async Task<IActionResult> Delete(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound();

            if (lesson.Course == null ||
                !CanManage(lesson.Course))
                return Forbid();

            _context.Lessons.Remove(lesson);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa bài học thành công"
            });
        }
    }
}