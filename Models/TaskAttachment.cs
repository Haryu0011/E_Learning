using System.ComponentModel.DataAnnotations;

//About : This is for the teacher's learning material.

namespace E_Learning.Models
{
    public class TaskAttachment
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }

        public Task Task { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;
    }
}
