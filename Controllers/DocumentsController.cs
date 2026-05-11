using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
using Diplom_StudyHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public DocumentsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ✅ ПАГИНАЦИЯ (20 документов на страницу)
        public async Task<IActionResult> Index(int groupId, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound();

            int pageSize = 20;

            var documents = await _context.Documents
                .Include(d => d.Uploader)
                .Where(d => d.GroupId == groupId)
                .OrderByDescending(d => d.UploadedAt)
                .ToListAsync();

            var totalItems = documents.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paginatedDocuments = documents
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                ItemsPerPage = pageSize
            };

            return View(paginatedDocuments);
        }

        public IActionResult Upload(int groupId)
        {
            ViewBag.GroupId = groupId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int groupId, IFormFile file, string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

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

            // ✅ Проверка размера (макс. 50 МБ)
            const long maxFileSize = 50 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                ModelState.AddModelError("", "Размер файла не должен превышать 50 МБ");
                ViewBag.GroupId = groupId;
                return View();
            }

            // ✅ Проверка расширения
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".zip", ".rar", ".png", ".jpg", ".jpeg" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", "Недопустимый формат файла. Разрешены: PDF, DOC, DOCX, XLS, XLSX, PPT, PPTX, TXT, ZIP, RAR, PNG, JPG");
                ViewBag.GroupId = groupId;
                return View();
            }

            try
            {
                // ✅ Создание папки
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
                    Description = description,
                    UploadedAt = DateTime.Now
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { groupId });
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var document = await _context.Documents.FindAsync(id);
            if (document == null) return NotFound();

            var isOwner = document.UploaderId == user.Id;
            var isGroupOwner = await _context.Groups.AnyAsync(g => g.Id == groupId && g.OwnerId == user.Id);

            if (!isOwner && !isGroupOwner) return Forbid();

            try
            {
                var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при удалении: {ex.Message}");
            }

            return RedirectToAction("Index", new { groupId });
        }

        public async Task<IActionResult> Download(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var document = await _context.Documents
                .Include(d => d.Group)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == document.GroupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

            var filePath = Path.Combine(_environment.WebRootPath, document.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                ModelState.AddModelError("", "Файл не найден на сервере");
                return RedirectToAction("Index", new { groupId = document.GroupId });
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/octet-stream", document.FileName);
        }
    }
}