using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.DTOs;
using OnlineCourseManagement.Models;
using Microsoft.AspNetCore.Authorization;
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

        // GET: api/lessons
        [HttpGet]
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
                    l.Content,
                    l.VideoUrl,
                    l.DurationMinutes,
                    l.OrderIndex,
                    l.CourseId,
                    CourseName = l.Course != null
                        ? l.Course.Title : null
                })
                .ToListAsync();

            return Ok(lessons);
        }

        // GET: api/lessons/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null)
                return NotFound(new
                {
                    message = "Không tìm thấy bài học"
                });

            return Ok(lesson);
        }

        // GET: api/lessons/course/1
        [HttpGet("course/{courseId:int}")]
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
                .ToListAsync();

            return Ok(lessons);
        }

        // POST: api/lessons
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateLessonDto dto)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
                return BadRequest(new
                {
                    message = "Khóa học không tồn tại"
                });

            var orderExists = await _context.Lessons
                .AnyAsync(l =>
                    l.CourseId == dto.CourseId &&
                    l.OrderIndex == dto.OrderIndex);

            if (orderExists)
                return BadRequest(new
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

        // PUT: api/lessons/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            [FromBody] UpdateLessonDto dto)
        {
            var lesson = await _context.Lessons
                .FindAsync(id);

            if (lesson == null)
                return NotFound(new
                {
                    message = "Bài học không tồn tại"
                });

            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == dto.CourseId);

            if (!courseExists)
                return BadRequest(new
                {
                    message = "Khóa học không tồn tại"
                });

            var orderExists = await _context.Lessons
                .AnyAsync(l =>
                    l.Id != id &&
                    l.CourseId == dto.CourseId &&
                    l.OrderIndex == dto.OrderIndex);

            if (orderExists)
                return BadRequest(new
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

        // DELETE: api/lessons/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lesson = await _context.Lessons
                .FindAsync(id);

            if (lesson == null)
                return NotFound(new
                {
                    message = "Bài học không tồn tại"
                });

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Xóa bài học thành công"
            });
        }
    }
}