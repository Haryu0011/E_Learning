namespace E_Learning.ViewModels
{
    public class StudentHistoryViewModel
    {
        public int TaskId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime SubmittedAt { get; set; }

        public int? Score { get; set; }

        public bool IsGraded { get; set; }
    }
}