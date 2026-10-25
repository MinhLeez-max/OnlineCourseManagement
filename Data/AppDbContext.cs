using Microsoft.EntityFrameworkCore;
using OnlineCourseManagement.Models;

namespace OnlineCourseManagement.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(
            DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<User> Users { get; set; }
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany()
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Instructor)
                .WithMany()
                .HasForeignKey(c => c.InstructorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}