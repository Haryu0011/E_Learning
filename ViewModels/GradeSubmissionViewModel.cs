using System.ComponentModel.DataAnnotations;

namespace E_Learning.ViewModels
{
    public class GradeSubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public int TaskId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsLate { get; set; }

        public List<SubmissionAttachmentViewModel> Attachments { get; set; } = new();

        public bool IsGraded { get; set; }

        [Range(0, 100, ErrorMessage = "Nilai harus antara 0 dan 100.")]
        public int Score { get; set; }
    }
}