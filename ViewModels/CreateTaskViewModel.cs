using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace E_Learning.ViewModels
{
    public class CreateTaskViewModel
    {
        [Required(ErrorMessage = "Judul tugas wajib diisi.")]
        [StringLength(200)]
        [Display(Name = "Judul Tugas")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mata pelajaran wajib diisi.")]
        [StringLength(100)]
        [Display(Name = "Mata Pelajaran")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Deskripsi tugas wajib diisi.")]
        [Display(Name = "Deskripsi")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Batas waktu wajib diisi.")]
        [Display(Name = "Batas Pengumpulan")]
        public DateTime DueDate { get; set; }

        [Display(Name = "Lampiran")]
        public List<IFormFile>? Attachments { get; set; }
    }
}