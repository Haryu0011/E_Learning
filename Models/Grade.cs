using System.ComponentModel.DataAnnotations;

//About : This represents the teacher's final grade.

namespace E_Learning.Models
{
    public class Grade
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        public Submission Submission { get; set; } = null!;

        [Required]
        [Range(0, 100)]
        public int Score { get; set; }

        public DateTime GradedAt { get; set; } = DateTime.Now;
    }
}
