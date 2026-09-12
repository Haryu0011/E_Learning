namespace E_Learning.ViewModels
{
    public class TeacherSubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public string StudentEmail { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public DateTime DueDate { get; set; }

        public bool IsLate { get; set; }

        public bool IsGraded { get; set; }

        public int? Score { get; set; }
    }
}