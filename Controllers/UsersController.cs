using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IPasswordHasher<User> _hasher;

        private static readonly string[] ValidRoles =
        {
            "Admin", "Instructor", "Student"
        };

        public UsersController(
            AppDbContext context,
            IPasswordHasher<User> hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        // GET: api/users
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? role,
            [FromQuery] string? search)
        {
            var query = _context.Users
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));
            }

            var users = await query
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role,
                    u.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản"
                });

            return Ok(user);
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> Create(
            [FromBody] CreateUserDto dto)
        {
            var role = ValidRoles.FirstOrDefault(
                r => r.Equals(
                    dto.Role?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (role == null)
                return BadRequest(new
                {
                    message = "Vai trò không hợp lệ"
                });

            var email = dto.Email.Trim().ToLowerInvariant();

            if (await _context.Users
                .AnyAsync(u => u.Email == email))
            {
                return Conflict(new
                {
                    message = "Email đã tồn tại"
                });
            }

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Role = role,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _hasher.HashPassword(
                user, dto.Password);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể tạo tài khoản"
                });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role
                });
        }

        // PUT: api/users/1/role
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(
            int id,
            [FromBody] UpdateUserRoleDto dto)
        {
            var role = ValidRoles.FirstOrDefault(
                r => r.Equals(
                    dto.Role?.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (role == null)
                return BadRequest(new
                {
                    message = "Vai trò không hợp lệ"
                });

            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new
                {
                    message = "Tài khoản không tồn tại"
                });

            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId == id.ToString() &&
                role != "Admin")
            {
                return BadRequest(new
                {
                    message = "Không thể tự hạ quyền Admin"
                });
            }

            if (user.Role == "Admin" && role != "Admin")
            {
                var adminCount = await _context.Users
                    .CountAsync(u => u.Role == "Admin");

                if (adminCount <= 1)
                    return BadRequest(new
                    {
                        message = "Phải giữ ít nhất một Admin"
                    });
            }

            user.Role = role;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật vai trò thành công",
                user.Id,
                user.Role
            });
        }

        // DELETE: api/users/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
                return NotFound(new
                {
                    message = "Không tìm thấy tài khoản"
                });

            var currentUserId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (currentUserId == id.ToString())
                return BadRequest(new
                {
                    message = "Không thể tự xóa tài khoản"
                });

            if (user.Role == "Admin")
            {
                var adminCount = await _context.Users
                    .CountAsync(u => u.Role == "Admin");

                if (adminCount <= 1)
                    return BadRequest(new
                    {
                        message = "Không thể xóa Admin cuối cùng"
                    });
            }

            _context.Users.Remove(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Tài khoản đang có dữ liệu liên quan, không thể xóa"
                });
            }

            return Ok(new
            {
                message = "Xóa tài khoản thành công"
            });
        }
    }
}