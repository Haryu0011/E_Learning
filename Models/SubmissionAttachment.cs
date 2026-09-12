using System.ComponentModel.DataAnnotations;

//About : This is where the student's uploaded files go.

namespace E_Learning.Models
{
    public class SubmissionAttachment
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        public Submission Submission { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;
    }
}
