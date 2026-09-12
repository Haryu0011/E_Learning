using E_Learning.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<E_Learning.Models.Task> Tasks { get; set; }

        public DbSet<TaskAttachment> TaskAttachments { get; set; }

        public DbSet<Submission> Submissions { get; set; }

        public DbSet<SubmissionAttachment> SubmissionAttachments { get; set; }

        public DbSet<Grade> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // One Task → Many TaskAttachments
            builder.Entity<TaskAttachment>()
                .HasOne(x => x.Task)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Task → Many Submissions
            builder.Entity<Submission>()
                .HasOne(x => x.Task)
                .WithMany(x => x.Submissions)
                .HasForeignKey(x => x.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Submission → Many SubmissionAttachments
            builder.Entity<SubmissionAttachment>()
                .HasOne(x => x.Submission)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // One Submission → One Grade
            builder.Entity<Grade>()
                .HasOne(x => x.Submission)
                .WithOne(x => x.Grade)
                .HasForeignKey<Grade>(x => x.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // One student can submit only once for each task
            builder.Entity<Submission>()
                .HasIndex(x => new { x.TaskId, x.StudentId })
                .IsUnique();

            /*Mechanism :
             * builder.Entity<Submission> exist to prevent:
            Abdul → Tugas 1 → Submission #1 => OK
            Abdul → Tugas 1 → Submission #2 => NG
             */
        }
    }
}