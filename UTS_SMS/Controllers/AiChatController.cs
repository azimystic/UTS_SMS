using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using UTS_SMS.Models;
using UTS_SMS.Services;
using UTS_SMS.ViewModels;

namespace UTS_SMS.Controllers
{
    [Authorize(Roles = "Admin,Teacher,Student")]
    public class AiChatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VectorStoreService _vectorStore;

        public AiChatController(
            UserManager<ApplicationUser> userManager,
            VectorStoreService vectorStore)
        {
            _userManager = userManager;
            _vectorStore = vectorStore;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.Contains("Admin") ? "Admin"
                            : roles.Contains("Teacher") ? "Teacher"
                            : roles.Contains("Student") ? "Student"
                            : roles.FirstOrDefault() ?? "Student";

            var model = new AiChatViewModel
            {
                UserName = user.FullName ?? user.Email ?? "User",
                UserRole = primaryRole,
                StudentId = user.StudentId,
                CampusId = user.CampusId,
                SuggestedQuestions = GetSuggestedQuestions(primaryRole)
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Documents()
        {
            try
            {
                var documents = await _vectorStore.GetIndexedDocumentsAsync();
                return Json(documents);
            }
            catch
            {
                return Json(new List<IndexedDocument>());
            }
        }

        private List<SuggestedQuestion> GetSuggestedQuestions(string role)
        {
            return role switch
            {
                "Student" => new List<SuggestedQuestion>
                {
                    new() { Icon = "fas fa-chart-bar", Text = "How did I do in my last exams?" },
                    new() { Icon = "fas fa-book", Text = "What topics should I study for the next test?" },
                    new() { Icon = "fas fa-calculator", Text = "What score do I need to get a B overall?" },
                    new() { Icon = "fas fa-clipboard-check", Text = "Show me my attendance summary" },
                    new() { Icon = "fas fa-graduation-cap", Text = "Which subjects am I strongest in?" },
                    new() { Icon = "fas fa-search", Text = "Find study material on any topic" }
                },
                "Teacher" => new List<SuggestedQuestion>
                {
                    new() { Icon = "fas fa-users", Text = "How is my class performing in the latest exam?" },
                    new() { Icon = "fas fa-chart-line", Text = "Which students need the most help?" },
                    new() { Icon = "fas fa-book-open", Text = "Search the syllabus for a specific topic" },
                    new() { Icon = "fas fa-trophy", Text = "Who are the top performers in my class?" },
                    new() { Icon = "fas fa-exclamation-triangle", Text = "Which students are at risk of failing?" },
                    new() { Icon = "fas fa-search", Text = "Find content in uploaded textbooks" }
                },
                "Admin" => new List<SuggestedQuestion>
                {
                    new() { Icon = "fas fa-school", Text = "Give me an overview of campus performance" },
                    new() { Icon = "fas fa-chart-pie", Text = "Compare performance across classes" },
                    new() { Icon = "fas fa-user-graduate", Text = "Look up a specific student's grades" },
                    new() { Icon = "fas fa-file-pdf", Text = "Search through all academic materials" },
                    new() { Icon = "fas fa-database", Text = "Ingest new PDF documents into the AI brain" },
                    new() { Icon = "fas fa-cogs", Text = "What data do you have access to?" }
                },
                _ => new List<SuggestedQuestion>()
            };
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Teacher")] // Only allow Admin/Teacher to upload
        public async Task<IActionResult> UploadPdf()
        {
            var file = Request.Form.Files.FirstOrDefault();
            if (file == null || file.Length == 0)
                return Json(new { success = false, error = "No file uploaded." });
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return Json(new { success = false, error = "Only PDF files are allowed." });

            // Save to wwwroot/uploads (ensure directory exists)
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);
            var fileName = Path.GetFileNameWithoutExtension(file.FileName) + "_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".pdf";
            var filePath = Path.Combine(uploadsPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Add to ChapterMaterials for ingestion
            var db = HttpContext.RequestServices.GetService<ApplicationDbContext>();
            var chapterMaterial = new ChapterMaterial
            {
                ChapterId = 1, // TODO: Assign correct chapter or let user pick
                Type = MaterialType.PDF,
                Heading = Path.GetFileNameWithoutExtension(file.FileName),
                FilePath = $"uploads/{fileName}",
                OriginalFileName = file.FileName,
                UploadedAt = DateTime.Now,
                UploadedBy = User.Identity?.Name ?? "AIChatUpload",
                IsActive = true
            };
            db.ChapterMaterials.Add(chapterMaterial);
            await db.SaveChangesAsync();

            // Ingest the PDF (extract, chunk, embed, store)
            var pdfIngest = HttpContext.RequestServices.GetService<PdfIngestionService>();
            try
            {
                await pdfIngest.IngestSinglePdfAsync(chapterMaterial.Id);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = "Upload succeeded but ingestion failed: " + ex.Message });
            }

            return Json(new { success = true, file = fileName });
        }
    }
}
