namespace E_Learning.ViewModels
{
    public class TeacherDashboardViewModel
    {
        public List<TeacherDashboardTaskViewModel> Tasks { get; set; } = new();

        public TeacherDashboardTaskViewModel? SelectedTask { get; set; }

        public TeacherDashboardSubmissionViewModel? SelectedSubmission { get; set; }
    }

    public class TeacherDashboardTaskViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<TeacherDashboardAttachmentViewModel> Attachments { get; set; } = new();

        public List<TeacherDashboardSubmissionViewModel> Submissions { get; set; } = new();
    }

    public class TeacherDashboardSubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public string StudentEmail { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public bool IsLate { get; set; }

        public bool IsGraded { get; set; }

        public int? Score { get; set; }
    }

    public class TeacherDashboardAttachmentViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;
    }
}