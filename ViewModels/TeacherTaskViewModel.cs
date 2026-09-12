namespace E_Learning.ViewModels
{
    public class TeacherTaskViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public int SubmissionCount { get; set; }

        public int GradedCount { get; set; }
    }
}