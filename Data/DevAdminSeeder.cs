using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Models;

namespace OnlineCourseManagement.Data
{
    public static class DevAdminSeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services,
            IConfiguration config)
        {
            var email = config["AdminSeed:Email"];
            var password = config["AdminSeed:Password"];

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
                return;

            email = email.Trim().ToLowerInvariant();

            using var scope = services.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

            var hasher = scope.ServiceProvider
                .GetRequiredService<IPasswordHasher<User>>();

            // Không thay đổi tài khoản đã tồn tại
            if (await db.Users.AnyAsync(
                u => u.Email == email))
                return;

            // Chỉ tạo Admin đầu tiên
            if (await db.Users.AnyAsync(
                u => u.Role == "Admin"))
                return;

            var admin = new User
            {
                FullName = "System Administrator",
                Email = email,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            admin.PasswordHash =
                hasher.HashPassword(admin, password);

            db.Users.Add(admin);
            await db.SaveChangesAsync();
        }
    }
}