using E_Learning.Data;
using E_Learning.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
    [Authorize(Roles = "Murid")]
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public StudentController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        //public async Task<IActionResult> Index(
        //string? status,
        //int? selectedId)
        //{
        //    var userId = _userManager.GetUserId(User);

        //    if (userId == null)
        //        return Challenge();

        //    var tasks = await _context.Tasks
        //        .Include(t => t.Submissions)
        //        .OrderBy(t => t.DueDate)
        //        .ToListAsync();

        //    var now = DateTime.Now;

        //    var taskList = tasks.Select(task =>
        //    {
        //        var submission = task.Submissions
        //            .FirstOrDefault(s => s.StudentId == userId);

        //        string taskStatus;

        //        if (submission != null)
        //        {
        //            taskStatus = "Terkirim";
        //        }
        //        else if (now > task.DueDate)
        //        {
        //            taskStatus = "Telat";
        //        }
        //        else
        //        {
        //            taskStatus = "Kosong";
        //        }

        //        return new StudentTaskViewModel
        //        {
        //            Id = task.Id,
        //            Title = task.Title,
        //            Subject = task.Subject,
        //            DueDate = task.DueDate,
        //            Status = taskStatus
        //        };
        //    }).ToList();

        //    if (!string.IsNullOrEmpty(status) && status != "Semua")
        //    {
        //        taskList = taskList
        //            .Where(t => t.Status == status)
        //            .ToList();
        //    }

        //    ViewBag.CurrentStatus = status ?? "Semua";

        //    return View(taskList);
        //}

        public async Task<IActionResult> Index(
        string? status,
        int? selectedId)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var tasks = await _context.Tasks
                .Include(t => t.Submissions)
                .OrderBy(t => t.DueDate)
                .ToListAsync();

            var now = DateTime.Now;

            var taskList = tasks.Select(task =>
            {
                var submission = task.Submissions
                    .FirstOrDefault(s => s.StudentId == userId);

                string taskStatus;

                if (submission != null)
                {
                    taskStatus = "Terkirim";
                }
                else if (now > task.DueDate)
                {
                    taskStatus = "Telat";
                }
                else
                {
                    taskStatus = "Kosong";
                }

                return new StudentTaskViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Subject = task.Subject,
                    DueDate = task.DueDate,
                    Status = taskStatus
                };
            }).ToList();

            if (!string.IsNullOrEmpty(status) && status != "Semua")
            {
                taskList = taskList
                    .Where(t => t.Status == status)
                    .ToList();
            }

            StudentTaskDetailViewModel? selectedTask = null;

            if (selectedId.HasValue)
            {
                var task = await _context.Tasks
                    .Include(t => t.Attachments)
                    .Include(t => t.Submissions)
                        .ThenInclude(s => s.Attachments)
                    .Include(t => t.Submissions)
                        .ThenInclude(s => s.Grade)
                    .FirstOrDefaultAsync(t => t.Id == selectedId.Value);

                if (task != null)
                {
                    var submission = task.Submissions
                        .FirstOrDefault(s => s.StudentId == userId);

                    selectedTask = new StudentTaskDetailViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Subject = task.Subject,
                        Description = task.Description,
                        DueDate = task.DueDate,

                        Attachments = task.Attachments
                            .Select(a => new TaskAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),

                        IsSubmitted = submission != null,
                        SubmittedAt = submission?.SubmittedAt,

                        SubmissionAttachments = submission?.Attachments
                            .Select(a => new SubmissionAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList() ?? new(),

                        Score = submission?.Grade?.Score
                    };
                }
            }

            ViewBag.CurrentStatus = status ?? "Semua";
            ViewBag.SelectedId = selectedId;

            var model = new StudentDashboardViewModel
            {
                Tasks = taskList,
                SelectedTask = selectedTask
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var task = await _context.Tasks
                .Include(t => t.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            var submission = task.Submissions
                .FirstOrDefault(s => s.StudentId == userId);

            var model = new StudentTaskDetailViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Subject = task.Subject,
                Description = task.Description,
                DueDate = task.DueDate,

                Attachments = task.Attachments
                    .Select(a => new TaskAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList(),

                IsSubmitted = submission != null,
                SubmittedAt = submission?.SubmittedAt,

                SubmissionAttachments = submission?.Attachments
                    .Select(a => new SubmissionAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList() ?? new(),

                Score = submission?.Grade?.Score
            };

            return View(model);
        }

        // old
        //[HttpGet]
        //public async Task<IActionResult> History()
        //{
        //    var studentId = _userManager.GetUserId(User);

        //    if (studentId == null)
        //        return Challenge();

        //    var submissions = await _context.Submissions
        //        .Where(s => s.StudentId == studentId)
        //        .Include(s => s.Task)
        //        .Include(s => s.Grade)
        //        .OrderByDescending(s => s.SubmittedAt)
        //        .ToListAsync();

        //    var model = submissions
        //        .Select(s => new StudentHistoryViewModel
        //        {
        //            TaskId = s.TaskId,
        //            Title = s.Task.Title,
        //            Subject = s.Task.Subject,
        //            SubmittedAt = s.SubmittedAt,
        //            Score = s.Grade?.Score,
        //            IsGraded = s.Grade != null
        //        })
        //        .ToList();

        //    return View(model);
        //}

        //new 
        [HttpGet]
        public async Task<IActionResult> History(int? selectedId)
        {
            var studentId = _userManager.GetUserId(User);

            if (studentId == null)
                return Challenge();

            var submissions = await _context.Submissions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Task)
                .Include(s => s.Grade)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();

            var items = submissions
                .Select(s => new StudentHistoryViewModel
                {
                    TaskId = s.TaskId,
                    Title = s.Task.Title,
                    Subject = s.Task.Subject,
                    SubmittedAt = s.SubmittedAt,
                    Score = s.Grade?.Score,
                    IsGraded = s.Grade != null
                })
                .ToList();

            StudentTaskDetailViewModel? selectedTask = null;

            if (selectedId.HasValue)
            {
                var task = await _context.Tasks
                    .Include(t => t.Attachments)
                    .Include(t => t.Submissions)
                        .ThenInclude(s => s.Attachments)
                    .Include(t => t.Submissions)
                        .ThenInclude(s => s.Grade)
                    .FirstOrDefaultAsync(t => t.Id == selectedId.Value);

                var submission = task?.Submissions
                    .FirstOrDefault(s => s.StudentId == studentId);

                // Riwayat only ever lists tasks the student has already
                // submitted, so only build a detail view when that holds true.
                if (task != null && submission != null)
                {
                    selectedTask = new StudentTaskDetailViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Subject = task.Subject,
                        Description = task.Description,
                        DueDate = task.DueDate,

                        Attachments = task.Attachments
                            .Select(a => new TaskAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),

                        IsSubmitted = true,
                        SubmittedAt = submission.SubmittedAt,

                        SubmissionAttachments = submission.Attachments
                            .Select(a => new SubmissionAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),

                        Score = submission.Grade?.Score
                    };
                }
            }

            ViewBag.SelectedId = selectedId;

            var model = new StudentHistoryPageViewModel
            {
                Items = items,
                SelectedTask = selectedTask
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int id,
            StudentTaskDetailViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // Prevent submitting more than once.
            var existingSubmission = await _context.Submissions
                .AnyAsync(s =>
                    s.TaskId == id &&
                    s.StudentId == userId);

            if (existingSubmission)
            {
                TempData["Error"] = "Tugas ini sudah dikumpulkan.";
                return RedirectToAction(nameof(Index), new { SelectedId = id });
            }

            // Prevent submission after the deadline.
            if (model.SubmissionFiles == null ||
                model.SubmissionFiles.Count == 0)
            {
                TempData["Error"] = "Silakan pilih file jawaban.";
                return RedirectToAction(nameof(Index), new { SelectedId = id });
            }

            var submission = new E_Learning.Models.Submission
            {
                TaskId = id,
                StudentId = userId,
                SubmittedAt = DateTime.Now
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "submissions",
                submission.Id.ToString());

            Directory.CreateDirectory(uploadFolder);

            foreach (var file in model.SubmissionFiles)
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

                var attachment = new E_Learning.Models.SubmissionAttachment
                {
                    SubmissionId = submission.Id,
                    FileName = fileName,
                    FilePath =
                        $"/uploads/submissions/{submission.Id}/{uniqueFileName}"
                };

                _context.SubmissionAttachments.Add(attachment);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Tugas berhasil dikumpulkan.";

            return RedirectToAction(nameof(Index), new { selectedId = id });
        }
    }
}