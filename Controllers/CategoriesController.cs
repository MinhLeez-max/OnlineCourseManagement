using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.Models;

namespace OnlineCourseManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoriesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .ToListAsync();

            return Ok(categories);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var category =
                await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            return Ok(category);
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] Category category)
        {
            category.Id = 0;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetById),
                new { id = category.Id },
                category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id, [FromBody] Category input)
        {
            var category =
                await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.Name = input.Name;
            category.Description = input.Description;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category =
                await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}