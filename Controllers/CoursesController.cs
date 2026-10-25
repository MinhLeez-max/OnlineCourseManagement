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
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CoursesController(AppDbContext context)
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

        private bool IsAdmin =>
            User.IsInRole("Admin");

        private bool CanManage(Course course)
        {
            return IsAdmin ||
                (User.IsInRole("Instructor") &&
                 course.InstructorId == CurrentUserId());
        }

        // GET: api/courses
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? search,
            [FromQuery] int? categoryId)
        {
            var query = _context.Courses
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Title.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(c =>
                    c.CategoryId == categoryId.Value);
            }

            var courses = await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.CategoryId,
                    CategoryName = c.Category != null
                        ? c.Category.Name : null,
                    c.InstructorId,
                    InstructorName = c.Instructor != null
                        ? c.Instructor.FullName : null,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/courses/1
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.Price,
                    c.CategoryId,
                    CategoryName = c.Category != null
                        ? c.Category.Name : null,
                    c.InstructorId,
                    InstructorName = c.Instructor != null
                        ? c.Instructor.FullName : null,
                    c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (course == null)
                return NotFound();

            return Ok(course);
        }

        // GET: api/courses/my-courses
        [Authorize(Roles = "Instructor")]
        [HttpGet("my-courses")]
        public async Task<IActionResult> MyCourses()
        {
            var userId = CurrentUserId();

            if (userId == null)
                return Unauthorized();

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.InstructorId == userId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Price,
                    c.CategoryId,
                    c.CreatedAt
                })
                .ToListAsync();

            return Ok(courses);
        }

        // POST: api/courses
        [Authorize(Roles = "Admin,Instructor")]
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateCourseDto dto)
        {
            var userId = CurrentUserId();

            if (userId == null)
                return Unauthorized();

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                return BadRequest(new
                {
                    message = "Danh mục không tồn tại"
                });

            int instructorId;

            if (IsAdmin)
            {
                if (!dto.InstructorId.HasValue)
                    return BadRequest(new
                    {
                        message = "Admin phải chọn giảng viên"
                    });

                instructorId = dto.InstructorId.Value;
            }
            else
            {
                if (dto.InstructorId.HasValue &&
                    dto.InstructorId.Value != userId.Value)
                {
                    return Forbid();
                }

                instructorId = userId.Value;
            }

            bool instructorExists = await _context.Users
                .AnyAsync(u =>
                    u.Id == instructorId &&
                    u.Role == "Instructor");

            if (!instructorExists)
                return BadRequest(new
                {
                    message = "Giảng viên không hợp lệ"
                });

            var course = new Course
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                InstructorId = instructorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = course.Id },
                new
                {
                    message = "Tạo khóa học thành công",
                    course.Id,
                    course.Title,
                    course.InstructorId
                });
        }

        // PUT: api/courses/1
        [Authorize(Roles = "Admin,Instructor")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateCourseDto dto)
        {
            var course = await _context.Courses
                .FindAsync(id);

            if (course == null)
                return NotFound();

            if (!CanManage(course))
                return Forbid();

            bool categoryExists = await _context.Categories
                .AnyAsync(c => c.Id == dto.CategoryId);

            if (!categoryExists)
                return BadRequest(new
                {
                    message = "Danh mục không tồn tại"
                });

            if (IsAdmin && dto.InstructorId.HasValue)
            {
                bool valid = await _context.Users
                    .AnyAsync(u =>
                        u.Id == dto.InstructorId.Value &&
                        u.Role == "Instructor");

                if (!valid)
                    return BadRequest(new
                    {
                        message = "Giảng viên không hợp lệ"
                    });

                course.InstructorId = dto.InstructorId.Value;
            }
            else if (!IsAdmin &&
                dto.InstructorId.HasValue &&
                dto.InstructorId != course.InstructorId)
            {
                return Forbid();
            }

            course.Title = dto.Title;
            course.Description = dto.Description;
            course.Price = dto.Price;
            course.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật khóa học thành công"
            });
        }

        // DELETE: api/courses/1
        [Authorize(Roles = "Admin,Instructor")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _context.Courses
                .FindAsync(id);

            if (course == null)
                return NotFound();

            if (!CanManage(course))
                return Forbid();

            // Không cho xóa khi đã có bài học.
            bool hasLessons = await _context.Lessons
                .AnyAsync(l => l.CourseId == id);

            if (hasLessons)
                return Conflict(new
                {
                    message =
                        "Hãy xóa hoặc xử lý bài học trước khi xóa khóa học"
                });

            _context.Courses.Remove(course);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa khóa học thành công"
            });
        }
    }
}