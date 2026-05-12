using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class DocumentsController : BaseController
    {
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IWebHostEnvironment environment)
            : base(context, userManager)
        {
            _environment = environment;
        }

        public async Task<IActionResult> Index(int groupId, int page = 1)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var documents = await _context.Documents
                .Include(d => d.Uploader)
                .Where(d => d.GroupId == groupId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            return View(documents);
        }

        public async Task<IActionResult> Upload(int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            ViewBag.GroupId = groupId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int groupId, IFormFile? file, string? description)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var user = await GetCurrentUserAsync();
            var group = await _context.Groups.FindAsync(groupId);

            if (group == null || group.Status == GroupStatus.Archived)
            {
                ModelState.AddModelError("", "Нельзя загружать файлы в архивированную группу");
                ViewBag.GroupId = groupId;
                return View();
            }

            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Выберите файл для загрузки");
                ViewBag.GroupId = groupId;
                return View();
            }

            const long maxFileSize = 50 * 1024 * 1024; // 50 MB
            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError("", "Размер файла не должен превышать 50 МБ");
                ViewBag.GroupId = groupId;
                return View();
            }

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Недопустимый формат файла");
                ViewBag.GroupId = groupId;
                return View();
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "documents", groupId.ToString());
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var document = new Document
                {
                    GroupId = groupId,
                    UploaderId = user.Id,
                    FileName = file.FileName,
                    FilePath = Path.Combine("uploads", "documents", groupId.ToString(), uniqueFileName),
                    FileSize = file.Length,
                    Description = description?.Trim(),
                    UploadedAt = DateTime.UtcNow
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { groupId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при загрузке файла: {ex.Message}");
                ViewBag.GroupId = groupId;
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            var user = await GetCurrentUserAsync();
            var isOwner = document.UploaderId == user.Id;
            var isGroupOwner = await IsGroupOwnerAsync(groupId);

            if (!isOwner && !isGroupOwner) return Forbid();

            try
            {
                var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при удалении: {ex.Message}");
            }

            return RedirectToAction(nameof(Index), new { groupId });
        }

        public async Task<IActionResult> Download(int id)
        {
            var document = await _context.Documents
                .Include(d => d.Group)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound();
            if (!await IsGroupMemberOrOwnerAsync(document.GroupId))
                return Forbid();

            var fullPath = Path.Combine(_environment.WebRootPath, document.FilePath);
            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var memory = new MemoryStream();
            using (var stream = new FileStream(fullPath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", document.FileName);
        }
    }
}