using System.ComponentModel.DataAnnotations;

//About : This is where the student's actual submission goes.

namespace E_Learning.Models
{
    public class Submission
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        public Task Task { get; set; } = null!;

        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public List<SubmissionAttachment> Attachments { get; set; }
            = new List<SubmissionAttachment>();

        public Grade? Grade { get; set; }
    }
}
