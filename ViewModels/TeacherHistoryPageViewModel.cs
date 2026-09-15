namespace E_Learning.ViewModels
{
    public class TeacherHistoryPageViewModel
    {
        public List<TeacherHistoryStudentViewModel> Students { get; set; } = new();

        public TeacherHistoryTaskDetailViewModel? SelectedTask { get; set; }

        public TeacherHistorySubmissionDetailViewModel? SelectedSubmission { get; set; }
    }

    public class TeacherHistoryStudentViewModel
    {
        public string StudentId { get; set; } = string.Empty;

        public string StudentEmail { get; set; } = string.Empty;

        public List<TeacherHistoryStudentSubmissionViewModel> Submissions { get; set; } = new();
    }

    public class TeacherHistoryStudentSubmissionViewModel
    {
        public int SubmissionId { get; set; }

        public int TaskId { get; set; }

        public string TaskTitle { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public bool IsLate { get; set; }

        public bool IsGraded { get; set; }

        public int? Score { get; set; }
    }

    public class TeacherHistoryTaskDetailViewModel
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<TeacherHistoryFileViewModel> Attachments { get; set; } = new();
    }

    public class TeacherHistorySubmissionDetailViewModel
    {
        public int SubmissionId { get; set; }

        public string StudentEmail { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public bool IsLate { get; set; }

        public bool IsGraded { get; set; }

        public int? Score { get; set; }

        public List<TeacherHistoryFileViewModel> Attachments { get; set; } = new();
    }

    public class TeacherHistoryFileViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;
    }
}