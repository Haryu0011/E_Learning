using Microsoft.AspNetCore.Http;

namespace E_Learning.ViewModels
{
    public class StudentTaskDetailViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<TaskAttachmentViewModel> Attachments { get; set; } = new();

        public bool IsSubmitted { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public List<SubmissionAttachmentViewModel> SubmissionAttachments { get; set; } = new();

        public int? Score { get; set; }

        public List<IFormFile>? SubmissionFiles { get; set; }
    }

    public class TaskAttachmentViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;
    }

    public class SubmissionAttachmentViewModel
    {
        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;
    }
}