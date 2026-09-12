using System.ComponentModel.DataAnnotations;

//About : This represents the actual assignment created by the teacher.

namespace E_Learning.Models
{
    public class Task
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Identity user ID of the teacher who created this task
        [Required]
        public string TeacherId { get; set; } = string.Empty;

        public List<TaskAttachment> Attachments { get; set; }
            = new List<TaskAttachment>();

        public List<Submission> Submissions { get; set; }
            = new List<Submission>();
    }
}
