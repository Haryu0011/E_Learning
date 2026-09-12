namespace E_Learning.ViewModels
{
    public class StudentDashboardViewModel
    {
        public List<StudentTaskViewModel> Tasks { get; set; } = new();

        public StudentTaskDetailViewModel? SelectedTask { get; set; }
    }
}