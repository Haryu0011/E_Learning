namespace E_Learning.ViewModels
{
    public class TeacherHistoryViewModel
    {
        public int TaskId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<TeacherHistorySubmissionViewModel> Submissions { get; set; } = new();
    }

    public class TeacherHistorySubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public string StudentEmail { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public bool IsLate { get; set; }

        public bool IsGraded { get; set; }

        public int? Score { get; set; }
    }
}