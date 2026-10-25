using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Data;
using OnlineCourseManagement.DTOs;
using OnlineCourseManagement.Models;
using OnlineCourseManagement.Services;
using System.Security.Claims;

namespace OnlineCourseManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;
        private readonly IPasswordHasher<User> _passwordHasher;

        public AuthController(
            AppDbContext context,
            JwtService jwtService,
            IPasswordHasher<User> passwordHasher)
        {
            _context = context;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
        }

        // POST: api/auth/register
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var exists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (exists)
                return Conflict(new
                {
                    message = "Email đã được sử dụng"
                });

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Role = "Student"
            };

            user.PasswordHash = _passwordHasher
                .HashPassword(user, dto.Password);

            _context.Users.Add(user);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể tạo tài khoản. Hãy kiểm tra email."
                });
            }

            return Ok(new
            {
                message = "Đăng ký thành công",
                user.Id,
                user.FullName,
                user.Email,
                user.Role
            });
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto)
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return Unauthorized(new
                {
                    message = "Email hoặc mật khẩu không đúng"
                });

            var result = _passwordHasher
                .VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized(new
                {
                    message = "Email hoặc mật khẩu không đúng"
                });

            if (result ==
                PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher
                    .HashPassword(user, dto.Password);

                await _context.SaveChangesAsync();
            }

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                message = "Đăng nhập thành công",
                token,
                user = new
                {
                    user.Id,
                    user.FullName,
                    user.Email,
                    user.Role
                }
            });
        }

        // GET: api/auth/me
        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idText = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!int.TryParse(idText, out var id))
                return Unauthorized();

            var user = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == id)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return Unauthorized();

            return Ok(user);
        }

        // GET: api/auth/student-only
        [Authorize(Roles = "Student")]
        [HttpGet("student-only")]
        public IActionResult StudentOnly()
        {
            return Ok(new
            {
                message = "Bạn có quyền Student"
            });
        }

        // GET: api/auth/admin-only
        [Authorize(Roles = "Admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            return Ok(new
            {
                message = "Bạn có quyền Admin"
            });
        }
    }
}