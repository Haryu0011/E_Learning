using E_Learning.Data;
using E_Learning.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
    [Authorize(Roles = "Guru")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public TeacherController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var tasks = await _context.Tasks
                .Where(t => t.TeacherId == teacherId)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var model = tasks.Select(task => new TeacherTaskViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Subject = task.Subject,
                DueDate = task.DueDate,
                SubmissionCount = task.Submissions.Count,
                GradedCount = task.Submissions.Count(s => s.Grade != null)
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var task = new E_Learning.Models.Task
            {
                Title = model.Title,
                Subject = model.Subject,
                Description = model.Description,
                DueDate = model.DueDate,
                CreatedAt = DateTime.Now,
                TeacherId = teacherId
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            if (model.Attachments != null && model.Attachments.Count > 0)
            {
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "tasks",
                    task.Id.ToString());

                Directory.CreateDirectory(uploadFolder);

                foreach (var file in model.Attachments)
                {
                    if (file.Length <= 0)
                        continue;

                    var fileName = Path.GetFileName(file.FileName);
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

                    var filePath = Path.Combine(
                        uploadFolder,
                        uniqueFileName);

                    using var stream = new FileStream(
                        filePath,
                        FileMode.Create);

                    await file.CopyToAsync(stream);

                    var attachment = new E_Learning.Models.TaskAttachment
                    {
                        TaskId = task.Id,
                        FileName = fileName,
                        FilePath = $"/uploads/tasks/{task.Id}/{uniqueFileName}"
                    };

                    _context.TaskAttachments.Add(attachment);
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Submissions(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var task = await _context.Tasks
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.TeacherId == teacherId);

            if (task == null)
                return NotFound();

            var studentIds = task.Submissions
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();

            var students = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.Email ?? u.UserName ?? "");

            var model = task.Submissions
                .OrderByDescending(s => s.SubmittedAt)
                .Select(s => new TeacherSubmissionViewModel
                {
                    SubmissionId = s.Id,
                    StudentEmail = students.TryGetValue(
                        s.StudentId,
                        out var email)
                            ? email
                            : "Unknown",

                    SubmittedAt = s.SubmittedAt,
                    DueDate = task.DueDate,
                    IsLate = s.SubmittedAt > task.DueDate,

                    IsGraded = s.Grade != null,
                    Score = s.Grade?.Score
                })
                .ToList();

            ViewBag.TaskTitle = task.Title;
            //ViewBag.TaskId = task.Id;

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var submission = await _context.Submissions
                .Include(s => s.Task)
                .Include(s => s.Attachments)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            // Make sure this Guru owns the task.
            if (submission.Task.TeacherId != teacherId)
                return Forbid();

            var student = await _userManager.FindByIdAsync(
                submission.StudentId);

            var model = new GradeSubmissionViewModel
            {
                SubmissionId = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task.Title,
                StudentEmail = student?.Email ?? student?.UserName ?? "Unknown",
                SubmittedAt = submission.SubmittedAt,
                DueDate = submission.Task.DueDate,
                IsLate = submission.SubmittedAt > submission.Task.DueDate,

                Attachments = submission.Attachments
        .Select(a => new SubmissionAttachmentViewModel
        {
            FileName = a.FileName,
            FilePath = a.FilePath
        })
        .ToList(),

                Score = submission.Grade?.Score ?? 0,
                IsGraded = submission.Grade != null
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(
        int id,
        GradeSubmissionViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var submission = await _context.Submissions
                .Include(s => s.Task)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null)
                return NotFound();

            // Make sure this Guru owns the task.
            if (submission.Task.TeacherId != teacherId)
                return Forbid();

            if (!ModelState.IsValid)
            {
                var student = await _userManager.FindByIdAsync(
                    submission.StudentId);

                model.StudentEmail =
                    student?.Email ?? student?.UserName ?? "Unknown";

                model.TaskTitle = submission.Task.Title;
                model.SubmittedAt = submission.SubmittedAt;
                model.DueDate = submission.Task.DueDate;
                model.IsLate = submission.SubmittedAt > submission.Task.DueDate;

                var attachments = await _context.SubmissionAttachments
                    .Where(a => a.SubmissionId == submission.Id)
                    .ToListAsync();

                model.Attachments = attachments
                    .Select(a => new SubmissionAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList();

                return View(model);
            }

            // A grade cannot be edited once it exists.
            if (submission.Grade != null)
            {
                TempData["Error"] = "Tugas ini sudah dinilai dan tidak dapat diubah.";

                return RedirectToAction(
                    nameof(Grade),
                    new { id });
            }

            var grade = new E_Learning.Models.Grade
            {
                SubmissionId = submission.Id,
                Score = model.Score,
                GradedAt = DateTime.Now
            };

            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Nilai berhasil disimpan.";

            return RedirectToAction(
                nameof(Submissions),
                new { id = submission.TaskId });
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();

            var tasks = await _context.Tasks
                .Where(t => t.TeacherId == teacherId)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var studentIds = tasks
                .SelectMany(t => t.Submissions)
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();

            var students = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.Email ?? u.UserName ?? "Unknown");

            var model = tasks.Select(task => new TeacherHistoryViewModel
            {
                TaskId = task.Id,
                TaskTitle = task.Title,
                Subject = task.Subject,
                DueDate = task.DueDate,

                Submissions = task.Submissions
                    .OrderByDescending(s => s.SubmittedAt)
                    .Select(s => new TeacherHistorySubmissionViewModel
                    {
                        SubmissionId = s.Id,

                        StudentEmail = students.TryGetValue(
                            s.StudentId,
                            out var email)
                                ? email
                                : "Unknown",

                        SubmittedAt = s.SubmittedAt,

                        IsLate = s.SubmittedAt > task.DueDate,

                        IsGraded = s.Grade != null,

                        Score = s.Grade?.Score
                    })
                    .ToList()
            }).ToList();

            return View(model);
        }
    }
}