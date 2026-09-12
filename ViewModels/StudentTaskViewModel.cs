namespace E_Learning.ViewModels
{
    public class StudentTaskViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public string Status { get; set; } = string.Empty;
    }
}