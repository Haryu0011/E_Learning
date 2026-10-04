using E_Learning.Data;
using E_Learning.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
    // ============================================================
    // STUDENT CONTROLLER
    // ============================================================
    //
    // Handles functionality available to users with the "Murid"
    // (Student) role.
    //
    // Info :
    //   - Students can only see their own tasks, submission, answer, and grades. 
    //   - "Available" task refer to tasks without grade; While "History" refer to tasks with grade.
    //   - Cannot send task answer with empty attachment(s).
    //
    // Main responsibilities:
    //   - Display available tasks - Menampilkan semua tugas yang tersedia
    //   - Filter tasks by submission status - Memfilter tugas berdasarkan status tugas
    //   - Display task details and attachments - Menampilkan detail tugas dan lampiran
    //   - Display submission history - Menampilkan riwayat tugas
    //   - Submit task answers and attachments - Mengirim jawaban tugas beserta lampiran tugas
    //
    // All actions in this controller require the authenticated
    // user to have the "Murid" role.
    //
    [Authorize(Roles = "Murid")]
    public class StudentController : Controller
    {
        // Entity Framework database context.
        // Used to access tasks, submissions, grades, and attachments.
        private readonly ApplicationDbContext _context;

        // ASP.NET Core Identity UserManager.
        // Used to identify the currently logged-in student.
        private readonly UserManager<IdentityUser> _userManager;

        // Provides access to the application's hosting environment.
        // Used here to determine where uploaded submission files
        // should be stored.
        private readonly IWebHostEnvironment _environment;


        // ========================================================
        // CONSTRUCTOR
        // ========================================================
        //
        // Dependencies are supplied automatically by ASP.NET Core's
        // Dependency Injection system.
        //
        public StudentController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        // ========================================================
        // STUDENT DASHBOARD
        // ========================================================
        //
        // Displays all available tasks and determines the student's
        // current status for each task.
        //
        // Intended task statuses:
        //
        //   "Terkirim" = Submitted
        //   "Telat"    = Past the deadline and not submitted
        //   "Kosong"   = Not submitted and still available
        //
        // status:
        //   Optional filter used to display only tasks with a
        //   particular status.
        //
        // selectedId:
        //   Optional task ID used to display task details.
        //
        [HttpGet]
        public async Task<IActionResult> Index(
            string? status,
            int? selectedId)
        {
            // Get the ID of the currently logged-in student.
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();


            // ----------------------------------------------------
            // Load all tasks.
            // ----------------------------------------------------
            //
            // Submissions are included so that the application can
            // determine whether the current student has submitted
            // each task.
            //
            var tasks = await _context.Tasks
                .Include(t => t.Submissions)
                .OrderBy(t => t.DueDate)
                .ToListAsync();


            var now = DateTime.Now;


            // ----------------------------------------------------
            // Determine the current student's status for each task.
            // ----------------------------------------------------
            //
            var taskList = tasks.Select(task =>
            {
                // Find this student's submission for the task.
                var submission = task.Submissions
                    .FirstOrDefault(s => s.StudentId == userId);

                string taskStatus;


                // If a submission exists, the task has been submitted.
                if (submission != null)
                {
                    taskStatus = "Terkirim";
                }

                // If there is no submission and the deadline has passed,
                // the task is considered late.
                else if (now > task.DueDate)
                {
                    taskStatus = "Telat";
                }

                // Otherwise, the task has not yet been submitted and
                // its deadline has not passed.
                else
                {
                    taskStatus = "Kosong";
                }


                // Convert the database entity into the ViewModel
                // required by the student dashboard.
                return new StudentTaskViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Subject = task.Subject,
                    DueDate = task.DueDate,
                    Status = taskStatus
                };

            }).ToList();


            // ----------------------------------------------------
            // Filter tasks by status.
            // ----------------------------------------------------
            //
            // "Semua" means "All", so no filtering is applied
            // when that value is selected.
            //
            if (!string.IsNullOrEmpty(status) && status != "Semua")
            {
                taskList = taskList
                    .Where(t => t.Status == status)
                    .ToList();
            }


            // ----------------------------------------------------
            // Load the selected task details.
            // ----------------------------------------------------
            //
            // The dashboard can optionally display one task's
            // detailed information alongside the task list.
            //
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
                    // Find this student's submission, if one exists.
                    var submission = task.Submissions
                        .FirstOrDefault(s => s.StudentId == userId);


                    // Build the detailed task ViewModel.
                    selectedTask = new StudentTaskDetailViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Subject = task.Subject,
                        Description = task.Description,
                        DueDate = task.DueDate,


                        // Files attached by the teacher.
                        Attachments = task.Attachments
                            .Select(a => new TaskAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),


                        // Submission information for the current student.
                        IsSubmitted = submission != null,
                        SubmittedAt = submission?.SubmittedAt,


                        // Files submitted by the student.
                        SubmissionAttachments = submission?.Attachments
                            .Select(a => new SubmissionAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList() ?? new(),


                        // Display the grade if the teacher has graded
                        // the submission.
                        Score = submission?.Grade?.Score
                    };
                }
            }


            // Store the current UI selection so that the view can
            // preserve the selected filter and task.
            ViewBag.CurrentStatus = status ?? "Semua";
            ViewBag.SelectedId = selectedId;


            // Build the main dashboard ViewModel.
            var model = new StudentDashboardViewModel
            {
                Tasks = taskList,
                SelectedTask = selectedTask
            };


            return View(model);
        }


        // ========================================================
        // TASK DETAIL
        // ========================================================
        //
        // Displays detailed information about a specific task,
        // including:
        //
        //   - Task description - Deskripsi Tugas
        //   - Due date - Tenggat waktu 
        //   - Teacher attachments - Lampiran Tugas
        //   - Student's submission - Jawaban Tugas
        //   - Submitted files - Lampiran Jawaban Tugas
        //   - Grade - Nilai Jawaban Tugas
        //
        //  Conceptually : 
        //      Tugas (Collections of Jawaban Tugas)
        //          Contains (many) : Jawaban Tugas
        //              Jawaban Tugas (Collections of Lampiran Tugas)
        //                  Contains (maybe many) :  Lampiran tugas
        //
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();


            // Load the task and all information required by the
            // detail page.
            var task = await _context.Tasks
                .Include(t => t.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // Only retrieve the submission belonging to the
            // currently logged-in student.
            var submission = task.Submissions
                .FirstOrDefault(s => s.StudentId == userId);


            // Convert the task and student's submission into
            // the ViewModel used by the detail page.
            var model = new StudentTaskDetailViewModel
            {
                Id = task.Id,
                Title = task.Title,
                Subject = task.Subject,
                Description = task.Description,
                DueDate = task.DueDate,


                // Teacher-provided attachments.
                Attachments = task.Attachments
                    .Select(a => new TaskAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList(),


                // Student submission information.
                IsSubmitted = submission != null,
                SubmittedAt = submission?.SubmittedAt,


                // Student's submitted files.
                SubmissionAttachments = submission?.Attachments
                    .Select(a => new SubmissionAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList() ?? new(),


                // Grade is null when the teacher has not graded
                // the submission yet.
                Score = submission?.Grade?.Score
            };


            return View(model);
        }


        // ========================================================
        // SUBMISSION HISTORY
        // ========================================================
        //
        // Displays the student's previous submissions.
        //
        // selectedId:
        //   Optional task ID used to display detailed information
        //   about a previous submission.
        //
        [HttpGet]
        public async Task<IActionResult> History(int? selectedId)
        {
            var studentId = _userManager.GetUserId(User);

            if (studentId == null)
                return Challenge();


            // ----------------------------------------------------
            // Load the student's submissions.
            // ----------------------------------------------------
            //
            // Only submissions belonging to the current student
            // are retrieved.
            //
            var submissions = await _context.Submissions
                .Where(s => s.StudentId == studentId)
                .Include(s => s.Task)
                .Include(s => s.Grade)
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync();


            // Convert submissions into the history list ViewModel.
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


            // ----------------------------------------------------
            // Load the selected submission's task details.
            // ----------------------------------------------------
            //
            // A task can only be shown in the history detail panel
            // when the current student actually submitted it.
            //
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


                // History only contains tasks the student has already
                // submitted, so only build the detail ViewModel when
                // both the task and the student's submission exist.
                if (task != null && submission != null)
                {
                    selectedTask = new StudentTaskDetailViewModel
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Subject = task.Subject,
                        Description = task.Description,
                        DueDate = task.DueDate,


                        // Teacher-provided task attachments.
                        Attachments = task.Attachments
                            .Select(a => new TaskAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),


                        // A history item is known to have been submitted.
                        IsSubmitted = true,
                        SubmittedAt = submission.SubmittedAt,


                        // Files submitted by the student.
                        SubmissionAttachments = submission.Attachments
                            .Select(a => new SubmissionAttachmentViewModel
                            {
                                FileName = a.FileName,
                                FilePath = a.FilePath
                            })
                            .ToList(),


                        // Display the grade when available.
                        Score = submission.Grade?.Score
                    };
                }
            }


            ViewBag.SelectedId = selectedId;


            // Build the complete history page ViewModel.
            var model = new StudentHistoryPageViewModel
            {
                Items = items,
                SelectedTask = selectedTask
            };


            return View(model);
        }


        // ========================================================
        // SUBMIT TASK
        // ========================================================
        //
        // Handles submission of a student's answer files.
        //
        // The action:
        //   1. Identifies the current student
        //   2. Finds the requested task
        //   3. Prevents duplicate submissions
        //   4. Validates that files were selected
        //   5. Creates the submission record
        //   6. Saves uploaded files
        //   7. Stores file metadata in the database
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(
            int id,
            StudentTaskDetailViewModel model)
        {
            // Get the currently authenticated student's ID.
            var userId = _userManager.GetUserId(User);

            if (userId == null)
                return Challenge();


            // ----------------------------------------------------
            // Find the requested task.
            // ----------------------------------------------------
            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id);


            if (task == null)
                return NotFound();


            // ----------------------------------------------------
            // Prevent duplicate submissions.
            // ----------------------------------------------------
            //
            // A student is allowed to submit a task only once.
            // This check uses both TaskId and StudentId so that
            // submissions from other students do not interfere.
            //
            var existingSubmission = await _context.Submissions
                .AnyAsync(s =>
                    s.TaskId == id &&
                    s.StudentId == userId);


            if (existingSubmission)
            {
                TempData["Error"] = "Tugas ini sudah dikumpulkan.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        SelectedId = id
                    });
            }


            // ----------------------------------------------------
            // Validate uploaded files.
            // ----------------------------------------------------
            //
            // The student must select at least one answer file
            // before a submission can be created.
            //
            if (model.SubmissionFiles == null ||
                model.SubmissionFiles.Count == 0)
            {
                TempData["Error"] = "Silakan pilih file jawaban.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        SelectedId = id
                    });
            }


            // ----------------------------------------------------
            // Create the submission record.
            // ----------------------------------------------------
            //
            // The StudentId is taken from the authenticated user
            // rather than from the submitted form.
            //
            var submission = new E_Learning.Models.Submission
            {
                TaskId = id,
                StudentId = userId,
                SubmittedAt = DateTime.Now
            };


            _context.Submissions.Add(submission);

            // Save first so that the submission receives its database ID.
            // The ID is then used as the upload directory name.
            await _context.SaveChangesAsync();


            // ----------------------------------------------------
            // Prepare the upload directory.
            // ----------------------------------------------------
            //
            // Student submissions are stored under:
            //
            // wwwroot/uploads/submissions/{submissionId}/
            //
            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "submissions",
                submission.Id.ToString());


            Directory.CreateDirectory(uploadFolder);


            // ----------------------------------------------------
            // Save each submitted file.
            // ----------------------------------------------------
            //
            foreach (var file in model.SubmissionFiles)
            {
                // Ignore empty files.
                if (file.Length <= 0)
                    continue;


                // Use only the filename portion supplied by the client.
                // This prevents directory path information from being
                // included in the stored filename.
                var fileName = Path.GetFileName(file.FileName);


                // Prefix the filename with a GUID so that files with
                // identical names do not overwrite one another.
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";


                var filePath = Path.Combine(
                    uploadFolder,
                    uniqueFileName);


                // Copy the uploaded file to the server.
                using var stream = new FileStream(
                    filePath,
                    FileMode.Create);

                await file.CopyToAsync(stream);


                // Store information about the uploaded file in the
                // database. The actual file is stored on disk.
                var attachment = new E_Learning.Models.SubmissionAttachment
                {
                    SubmissionId = submission.Id,
                    FileName = fileName,
                    FilePath =
                        $"/uploads/submissions/{submission.Id}/{uniqueFileName}"
                };


                _context.SubmissionAttachments.Add(attachment);
            }


            // Save all attachment records.
            await _context.SaveChangesAsync();


            TempData["SubmissionSuccess"] = true;


            // Return to the dashboard and keep the submitted task
            // selected so the student can immediately see its status.
            return RedirectToAction(
                nameof(Index),
                new
                {
                    selectedId = id
                });
        }
    }
}