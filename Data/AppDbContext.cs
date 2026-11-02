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
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LessonProgress> LessonProgresses { get; set; }
        public DbSet<UploadedFile> UploadedFiles { get; set; }
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
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasIndex(e => new
                {
                    e.StudentId,
                    e.CourseId
                }).IsUnique();

                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Course)
                    .WithMany()
                    .HasForeignKey(e => e.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<LessonProgress>(entity =>
            {
                // Không tạo hai bản ghi cho cùng bài học
                entity.HasIndex(p => new
                {
                    p.EnrollmentId,
                    p.LessonId
                }).IsUnique();

                entity.HasOne(p => p.Enrollment)
                    .WithMany()
                    .HasForeignKey(p => p.EnrollmentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Lesson)
                    .WithMany()
                    .HasForeignKey(p => p.LessonId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<UploadedFile>(entity =>
            {
                entity.HasOne(f => f.Course)
                    .WithMany()
                    .HasForeignKey(f => f.CourseId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Lesson)
                    .WithMany()
                    .HasForeignKey(f => f.LessonId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(f => f.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(f => new
                {
                    f.CourseId,
                    f.FileType
                });
            });
        }
    }
}