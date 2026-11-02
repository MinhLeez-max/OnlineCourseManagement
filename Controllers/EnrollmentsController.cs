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
    [Authorize]
    public class EnrollmentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EnrollmentsController(AppDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId()
        {
            var value = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            return int.TryParse(value, out int id)
                ? id : null;
        }

        // POST: api/enrollments
        // Student đăng ký khóa học
        [Authorize(Roles = "Student")]
        [HttpPost]
        public async Task<IActionResult> Enroll(
            [FromBody] EnrollCourseDto dto)
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var studentExists = await _context.Users
                .AnyAsync(u =>
                    u.Id == studentId.Value &&
                    u.Role == "Student");

            if (!studentExists)
                return Forbid();

            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
                return NotFound(new
                {
                    message = "Khóa học không tồn tại"
                });

            var alreadyEnrolled =
                await _context.Enrollments
                    .AnyAsync(e =>
                        e.StudentId == studentId.Value &&
                        e.CourseId == dto.CourseId);

            if (alreadyEnrolled)
                return Conflict(new
                {
                    message = "Bạn đã đăng ký khóa học này"
                });

            var enrollment = new Enrollment
            {
                StudentId = studentId.Value,
                CourseId = dto.CourseId,
                EnrolledAt = DateTime.UtcNow
            };

            _context.Enrollments.Add(enrollment);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message =
                        "Không thể đăng ký. Hãy kiểm tra trạng thái khóa học và thử lại."
                });
            }

            return StatusCode(201, new
            {
                message = "Đăng ký khóa học thành công",
                enrollment.Id,
                enrollment.StudentId,
                enrollment.CourseId,
                enrollment.EnrolledAt
            });
        }

        // GET: api/enrollments/my
        // Student xem các khóa học đã đăng ký
        [Authorize(Roles = "Student")]
        [HttpGet("my")]
        public async Task<IActionResult> MyCourses()
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId.Value)
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    EnrollmentId = e.Id,
                    e.CourseId,
                    CourseName = e.Course.Title,
                    e.Course.Description,
                    e.Course.Price,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // GET: api/enrollments
        // Admin xem tất cả lượt đăng ký
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .OrderByDescending(e => e.EnrolledAt)
                .Select(e => new
                {
                    e.Id,
                    e.StudentId,
                    StudentName = e.Student.FullName,
                    StudentEmail = e.Student.Email,
                    e.CourseId,
                    CourseName = e.Course.Title,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(enrollments);
        }

        // GET: api/enrollments/course/1
        // Admin hoặc giảng viên phụ trách xem học viên
        [Authorize(Roles = "Admin,Instructor")]
        [HttpGet("course/{courseId:int}")]
        public async Task<IActionResult> GetStudents(
            int courseId)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                return NotFound(new
                {
                    message = "Khóa học không tồn tại"
                });

            if (!User.IsInRole("Admin") &&
                course.InstructorId != CurrentUserId())
            {
                return Forbid();
            }

            var students = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .OrderBy(e => e.Student.FullName)
                .Select(e => new
                {
                    EnrollmentId = e.Id,
                    e.StudentId,
                    e.Student.FullName,
                    e.Student.Email,
                    e.EnrolledAt
                })
                .ToListAsync();

            return Ok(new
            {
                CourseId = course.Id,
                CourseName = course.Title,
                TotalStudents = students.Count,
                Students = students
            });
        }

        // DELETE: api/enrollments/{courseId}
        // Student hủy đăng ký khóa học
        [Authorize(Roles = "Student")]
        [HttpDelete("{courseId:int}")]
        public async Task<IActionResult> Cancel(int courseId)
        {
            var studentId = CurrentUserId();

            if (studentId == null)
                return Unauthorized();

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e =>
                    e.StudentId == studentId.Value &&
                    e.CourseId == courseId);

            if (enrollment == null)
                return NotFound(new
                {
                    message = "Bạn chưa đăng ký khóa học này"
                });

            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Hủy đăng ký thành công"
            });
        }
    }
}