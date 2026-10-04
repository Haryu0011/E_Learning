using E_Learning.Data;
using E_Learning.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_Learning.Controllers
{
    // ============================================================
    // TEACHER CONTROLLER
    // ============================================================
    //
    // Handles functionality available to users with the "Guru"
    // (Teacher) role.
    //
    // Main responsibilities:
    //   - Display the teacher dashboard - Menampilkan halaman dashboard guru
    //   - Create assignments - Membuat tugas
    //   - Upload task attachments - Mengirim lampiran tugas
    //   - View student submissions - Melihat semua jawaban tugas murid
    //   - Grade student submissions - Menilai semua jawaban tugas murid
    //   - Display submission history - Menampilkan riwayat penilaian
    //
    // All actions in this controller require the authenticated
    // user to have the "Guru" role.
    //
    [Authorize(Roles = "Guru")]
    public class TeacherController : Controller
    {
        // Entity Framework database context.
        // Used to access Tasks, Submissions, Grades, Attachments,
        // and other application data.
        private readonly ApplicationDbContext _context;

        // ASP.NET Core Identity UserManager.
        // Used to retrieve information about the currently logged-in
        // teacher and students.
        private readonly UserManager<IdentityUser> _userManager;

        // Provides access to the application's hosting environment,
        // including the wwwroot directory where uploaded files
        // are stored.
        private readonly IWebHostEnvironment _environment;


        // ========================================================
        // CONSTRUCTOR
        // ========================================================
        //
        // Dependencies are provided automatically by ASP.NET Core's
        // Dependency Injection system.
        //
        public TeacherController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }


        // ========================================================
        // TEACHER DASHBOARD
        // ========================================================
        //
        // Displays all tasks created by the currently logged-in
        // teacher, together with their submissions, grades, and
        // attachments.
        //
        // selectedTaskId:
        //   Optional ID of the task that should be selected in
        //   the dashboard.
        //
        // selectedSubmissionId:
        //   Optional ID of the submission that should be selected.
        //   The submission must belong to the selected task.
        //
        [HttpGet]
        public async Task<IActionResult> Index(
            int? selectedTaskId,
            int? selectedSubmissionId)
        {
            // Get the ID of the currently authenticated teacher.
            var teacherId = _userManager.GetUserId(User);

            // Normally this should not occur because the controller
            // requires authorization, but we still verify that a
            // valid user ID is available.
            if (teacherId == null)
                return Challenge();


            // ----------------------------------------------------
            // Load the teacher's tasks and related data.
            // ----------------------------------------------------
            //
            // Only tasks owned by the current teacher are loaded.
            //
            // Include():
            //   Loads task attachments and submissions.
            //
            // ThenInclude():
            //   Loads data related to each submission, such as
            //   its grade and submitted files.
            //
            var tasks = await _context.Tasks
                .Where(t => t.TeacherId == teacherId)
                .Include(t => t.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Attachments)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();


            // ----------------------------------------------------
            // Get the IDs of students who submitted work.
            // ----------------------------------------------------
            //
            // We first collect the unique student IDs from all
            // submissions so that their Identity information can
            // be retrieved in one database query.
            //
            var studentIds = tasks
                .SelectMany(t => t.Submissions)
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();


            // Retrieve student accounts from ASP.NET Core Identity
            // and store them in a dictionary for quick lookup.
            var students = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.Email ?? u.UserName ?? "Unknown");


            // ----------------------------------------------------
            // Build the ViewModel for the teacher dashboard.
            // ----------------------------------------------------
            //
            // Entity models are converted into ViewModels so that
            // the view receives only the data it needs to display.
            //
            var model = new TeacherDashboardViewModel
            {
                Tasks = tasks.Select(task => new TeacherDashboardTaskViewModel
                {
                    Id = task.Id,
                    Title = task.Title,
                    Subject = task.Subject,
                    Description = task.Description,
                    DueDate = task.DueDate,

                    // Convert task attachments into the ViewModel
                    // used by the dashboard.
                    Attachments = task.Attachments
                        .Select(a => new TeacherDashboardAttachmentViewModel
                        {
                            FileName = a.FileName,
                            FilePath = a.FilePath
                        })
                        .ToList(),

                    // Convert submissions into dashboard ViewModels.
                    Submissions = task.Submissions
                        .OrderByDescending(s => s.SubmittedAt)
                        .Select(s => new TeacherDashboardSubmissionViewModel
                        {
                            SubmissionId = s.Id,

                            // Look up the student's email from Identity.
                            StudentEmail = students.TryGetValue(
                                s.StudentId,
                                out var email)
                                    ? email
                                    : "Unknown",

                            SubmittedAt = s.SubmittedAt,

                            // A submission is considered late when it
                            // was submitted after the task's due date.
                            IsLate = s.SubmittedAt > task.DueDate,

                            // A submission is graded when a Grade record
                            // exists for it.
                            IsGraded = s.Grade != null,

                            Score = s.Grade?.Score,

                            // Include files submitted by the student.
                            Attachments = s.Attachments
                                .Select(a => new SubmissionAttachmentViewModel
                                {
                                    FileName = a.FileName,
                                    FilePath = a.FilePath
                                })
                                .ToList()
                        })
                        .ToList()
                }).ToList()
            };


            // ----------------------------------------------------
            // Select the task displayed by default.
            // ----------------------------------------------------
            //
            // If a task ID was supplied, select that task.
            // Otherwise, select the first available task.
            //
            model.SelectedTask = selectedTaskId.HasValue
                ? model.Tasks.FirstOrDefault(t => t.Id == selectedTaskId.Value)
                : model.Tasks.FirstOrDefault();


            // ----------------------------------------------------
            // Select a submission within the selected task.
            // ----------------------------------------------------
            //
            // The submission is searched inside the selected task
            // rather than globally. This ensures that the selected
            // submission belongs to the currently selected task.
            //
            if (model.SelectedTask != null &&
                selectedSubmissionId.HasValue)
            {
                model.SelectedSubmission =
                    model.SelectedTask.Submissions
                        .FirstOrDefault(
                            s => s.SubmissionId == selectedSubmissionId.Value);
            }

            return View(model);
        }


        // ========================================================
        // CREATE TASK - GET
        // ========================================================
        //
        // Displays the form used by a teacher to create a new task.
        //
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // ========================================================
        // CREATE TASK - POST
        // ========================================================
        //
        // Creates a new task and optionally saves uploaded
        // attachments to the server.
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTaskViewModel model)
        {
            // Validate the submitted form data before creating
            // anything in the database.
            if (!ModelState.IsValid)
            {
                TempData["Error"] =
                    "Tugas gagal dibuat, harap hubungi admin jika masalah berlanjut";

                return View(model);
            }


            // Get the currently logged-in teacher's ID.
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();


            // ----------------------------------------------------
            // Create the task entity.
            // ----------------------------------------------------
            //
            // The TeacherId is taken from the authenticated user
            // instead of the form so that the task is automatically
            // associated with the teacher creating it.
            //
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

            // Save first so that the new task receives its database ID.
            // The task ID is then used as part of the attachment folder path.
            await _context.SaveChangesAsync();


            // ----------------------------------------------------
            // Save uploaded task attachments.
            // ----------------------------------------------------
            //
            // Files are stored under:
            //
            // wwwroot/uploads/tasks/{taskId}/
            //
            // Each file receives a GUID prefix to reduce the chance
            // of filename collisions.
            //
            if (model.Attachments != null && model.Attachments.Count > 0)
            {
                var uploadFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "tasks",
                    task.Id.ToString());

                // Create the directory if it does not already exist.
                Directory.CreateDirectory(uploadFolder);


                foreach (var file in model.Attachments)
                {
                    // Ignore empty files.
                    if (file.Length <= 0)
                        continue;


                    // Get only the filename portion to prevent a client
                    // from supplying directory path information.
                    var fileName = Path.GetFileName(file.FileName);

                    // Add a unique identifier to avoid filename collisions.
                    var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";

                    var filePath = Path.Combine(
                        uploadFolder,
                        uniqueFileName);


                    // Copy the uploaded file to the server.
                    using var stream = new FileStream(
                        filePath,
                        FileMode.Create);

                    await file.CopyToAsync(stream);


                    // Store the file's metadata in the database.
                    // The actual file remains in the uploads directory.
                    var attachment = new E_Learning.Models.TaskAttachment
                    {
                        TaskId = task.Id,
                        FileName = fileName,
                        FilePath = $"/uploads/tasks/{task.Id}/{uniqueFileName}"
                    };

                    _context.TaskAttachments.Add(attachment);
                }

                // Save all attachment records after processing
                // the uploaded files.
                await _context.SaveChangesAsync();
            }


            TempData["Success"] = "Tugas berhasil dibuat";

            // Redirect after a successful POST to prevent the form
            // from being submitted again if the page is refreshed.
            return RedirectToAction(nameof(Index));
        }


        // ========================================================
        // VIEW SUBMISSIONS
        // ========================================================
        //
        // Displays all submissions belonging to a specific task.
        //
        // The task must belong to the currently logged-in teacher.
        //
        [HttpGet]
        public async Task<IActionResult> Submissions(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();


            // ----------------------------------------------------
            // Find the requested task.
            // ----------------------------------------------------
            //
            // IMPORTANT:
            // The teacher ID is included in the query itself.
            // This prevents a teacher from viewing submissions
            // belonging to another teacher's task simply by changing
            // the task ID in the URL.
            //
            var task = await _context.Tasks
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .FirstOrDefaultAsync(t =>
                    t.Id == id &&
                    t.TeacherId == teacherId);


            if (task == null)
                return NotFound();


            // Get the unique students who submitted this task.
            var studentIds = task.Submissions
                .Select(s => s.StudentId)
                .Distinct()
                .ToList();


            // Retrieve the students' Identity information.
            var students = await _userManager.Users
                .Where(u => studentIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => u.Email ?? u.UserName ?? "");


            // Convert database submissions into ViewModels.
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

                    // Determine whether the submission was late.
                    IsLate = s.SubmittedAt > task.DueDate,

                    IsGraded = s.Grade != null,
                    Score = s.Grade?.Score
                })
                .ToList();


            // Pass the task title separately to the view.
            ViewBag.TaskTitle = task.Title;

            return View(model);
        }


        // ========================================================
        // GRADE SUBMISSION - GET
        // ========================================================
        //
        // Displays the grading page for a specific submission.
        //
        [HttpGet]
        public async Task<IActionResult> Grade(int id)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();


            // Load the submission together with the task,
            // submitted files, and existing grade.
            var submission = await _context.Submissions
                .Include(s => s.Task)
                .Include(s => s.Attachments)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == id);


            if (submission == null)
                return NotFound();


            // ----------------------------------------------------
            // Verify task ownership.
            // ----------------------------------------------------
            //
            // A teacher should only be able to grade submissions
            // belonging to their own tasks.
            //
            if (submission.Task.TeacherId != teacherId)
                return Forbid();


            // Retrieve the student who submitted the work.
            var student = await _userManager.FindByIdAsync(
                submission.StudentId);


            // Build the ViewModel used by the grading page.
            var model = new GradeSubmissionViewModel
            {
                SubmissionId = submission.Id,
                TaskId = submission.TaskId,
                TaskTitle = submission.Task.Title,

                StudentEmail =
                    student?.Email ??
                    student?.UserName ??
                    "Unknown",

                SubmittedAt = submission.SubmittedAt,
                DueDate = submission.Task.DueDate,

                IsLate =
                    submission.SubmittedAt > submission.Task.DueDate,

                Attachments = submission.Attachments
                    .Select(a => new SubmissionAttachmentViewModel
                    {
                        FileName = a.FileName,
                        FilePath = a.FilePath
                    })
                    .ToList(),

                // If the submission already has a grade, display it.
                // Otherwise, default the score to zero.
                Score = submission.Grade?.Score ?? 0,

                IsGraded = submission.Grade != null
            };

            return View(model);
        }


        // ========================================================
        // GRADE SUBMISSION - POST
        // ========================================================
        //
        // Saves a grade for a student's submission.
        //
        // A submission can only be graded once. Existing grades
        // cannot be edited through this action.
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Grade(
            int id,
            GradeSubmissionViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();


            // Load the submission and its related task/grade.
            var submission = await _context.Submissions
                .Include(s => s.Task)
                .Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.Id == id);


            if (submission == null)
                return NotFound();


            // Make sure the current teacher owns the task.
            //
            // This is an important authorization check because the
            // submission ID comes from the request.
            if (submission.Task.TeacherId != teacherId)
                return Forbid();


            // ----------------------------------------------------
            // Validate the submitted grading form.
            // ----------------------------------------------------
            //
            // If validation fails, rebuild the information required
            // by the view before displaying the form again.
            //
            if (!ModelState.IsValid)
            {
                var student = await _userManager.FindByIdAsync(
                    submission.StudentId);

                model.StudentEmail =
                    student?.Email ??
                    student?.UserName ??
                    "Unknown";

                model.TaskTitle = submission.Task.Title;
                model.SubmittedAt = submission.SubmittedAt;
                model.DueDate = submission.Task.DueDate;
                model.IsLate =
                    submission.SubmittedAt > submission.Task.DueDate;


                // Reload submission attachments because they are
                // not necessarily included in the current query.
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


            // ----------------------------------------------------
            // Prevent an existing grade from being overwritten.
            // ----------------------------------------------------
            //
            // Once a grade exists, this application treats it as
            // final and does not allow it to be edited.
            //
            if (submission.Grade != null)
            {
                TempData["Error"] =
                    "Tugas ini sudah dinilai dan tidak dapat diubah.";

                return RedirectToAction(
                    nameof(Index),
                    new
                    {
                        selectedTaskId = submission.TaskId,
                        selectedSubmissionId = submission.Id
                    });
            }


            // Create a new grade associated with the submission.
            var grade = new E_Learning.Models.Grade
            {
                SubmissionId = submission.Id,
                Score = model.Score,
                GradedAt = DateTime.Now
            };

            _context.Grades.Add(grade);

            await _context.SaveChangesAsync();


            TempData["Success"] = "Nilai berhasil disimpan.";


            // Return to the dashboard and keep the graded submission
            // selected so the teacher can immediately see the result.
            return RedirectToAction(
                nameof(Index),
                new
                {
                    selectedTaskId = submission.TaskId,
                    selectedSubmissionId = submission.Id
                });
        }


        // ========================================================
        // SUBMISSION HISTORY
        // ========================================================
        //
        // Displays historical submission information grouped by
        // student.
        //
        // Optional selectedTaskId and selectedSubmissionId parameters
        // determine which task and submission are displayed in the
        // detail panels.
        //
        [HttpGet]
        public async Task<IActionResult> History(
            int? selectedTaskId,
            int? selectedSubmissionId)
        {
            var teacherId = _userManager.GetUserId(User);

            if (teacherId == null)
                return Challenge();


            // Load all tasks belonging to the current teacher,
            // including their submissions, grades, and attachments.
            var tasks = await _context.Tasks
                .Where(t => t.TeacherId == teacherId)
                .Include(t => t.Attachments)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Grade)
                .Include(t => t.Submissions)
                    .ThenInclude(s => s.Attachments)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();


            // Retrieve all users who have the "Murid" (Student) role.
            var muridUsers = await _userManager.GetUsersInRoleAsync("Murid");


            // Create a dictionary so tasks can be accessed efficiently
            // by their ID when processing submissions.
            var taskLookup = tasks.ToDictionary(t => t.Id);


            // ----------------------------------------------------
            // Build the student submission history.
            // ----------------------------------------------------
            //
            // Each student receives a list of their submissions,
            // ordered from newest to oldest.
            //
            var students = muridUsers
                .OrderBy(u => u.Email ?? u.UserName)
                .Select(student =>
                {
                    var submissions = tasks
                        .SelectMany(task => task.Submissions)
                        .Where(submission =>
                            submission.StudentId == student.Id)
                        .OrderByDescending(submission => submission.SubmittedAt)
                        .Select(submission =>
                        {
                            var task = taskLookup[submission.TaskId];

                            return new TeacherHistoryStudentSubmissionViewModel
                            {
                                SubmissionId = submission.Id,
                                TaskId = task.Id,
                                TaskTitle = task.Title,
                                Subject = task.Subject,
                                SubmittedAt = submission.SubmittedAt,

                                IsLate =
                                    submission.SubmittedAt > task.DueDate,

                                IsGraded = submission.Grade != null,
                                Score = submission.Grade?.Score
                            };
                        })
                        .ToList();


                    return new TeacherHistoryStudentViewModel
                    {
                        StudentId = student.Id,

                        StudentEmail =
                            student.Email ??
                            student.UserName ??
                            "Unknown",

                        Submissions = submissions
                    };
                })
                .ToList();


            // Create the main history page ViewModel.
            var model = new TeacherHistoryPageViewModel
            {
                Students = students
            };


            // ====================================================
            // SELECTED TASK
            // ====================================================
            //
            // If a task was selected, load its details into the
            // ViewModel so the view can display them.
            //
            if (selectedTaskId.HasValue)
            {
                var selectedTask = tasks
                    .FirstOrDefault(t => t.Id == selectedTaskId.Value);

                if (selectedTask != null)
                {
                    model.SelectedTask =
                        new TeacherHistoryTaskDetailViewModel
                        {
                            TaskId = selectedTask.Id,
                            Title = selectedTask.Title,
                            Subject = selectedTask.Subject,
                            Description = selectedTask.Description,
                            DueDate = selectedTask.DueDate,

                            Attachments = selectedTask.Attachments
                                .Select(a => new TeacherHistoryFileViewModel
                                {
                                    FileName = a.FileName,
                                    FilePath = a.FilePath
                                })
                                .ToList()
                        };
                }
            }


            // ====================================================
            // SELECTED SUBMISSION
            // ====================================================
            //
            // If a submission was selected, find it among the
            // teacher's tasks and populate the submission detail
            // ViewModel.
            //
            if (selectedSubmissionId.HasValue)
            {
                var submission = tasks
                    .SelectMany(t => t.Submissions)
                    .FirstOrDefault(s =>
                        s.Id == selectedSubmissionId.Value);

                if (submission != null)
                {
                    // Find the task associated with this submission.
                    var task = taskLookup[submission.TaskId];


                    // Retrieve the student's Identity account.
                    var student = await _userManager.FindByIdAsync(
                        submission.StudentId);


                    // Build the selected submission details.
                    model.SelectedSubmission =
                        new TeacherHistorySubmissionDetailViewModel
                        {
                            SubmissionId = submission.Id,

                            StudentEmail =
                                student?.Email ??
                                student?.UserName ??
                                "Unknown",

                            SubmittedAt = submission.SubmittedAt,

                            IsLate =
                                submission.SubmittedAt > task.DueDate,

                            IsGraded =
                                submission.Grade != null,

                            Score =
                                submission.Grade?.Score,

                            Attachments = submission.Attachments
                                .Select(a => new TeacherHistoryFileViewModel
                                {
                                    FileName = a.FileName,
                                    FilePath = a.FilePath
                                })
                                .ToList()
                        };


                    // The selected submission determines which task
                    // should be displayed in the task detail panel.
                    model.SelectedTask =
                        new TeacherHistoryTaskDetailViewModel
                        {
                            TaskId = task.Id,
                            Title = task.Title,
                            Subject = task.Subject,
                            Description = task.Description,
                            DueDate = task.DueDate,

                            Attachments = task.Attachments
                                .Select(a => new TeacherHistoryFileViewModel
                                {
                                    FileName = a.FileName,
                                    FilePath = a.FilePath
                                })
                                .ToList()
                        };
                }
            }

            return View(model);
        }
    }
}