using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class LessonsController : BaseController
    {
        public LessonsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index(int groupId, int page = 1)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var lessons = await _context.Lessons
                .Include(l => l.CreatedBy)
                .Where(l => l.GroupId == groupId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            return View(lessons);
        }

        public async Task<IActionResult> Create(int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            ViewBag.GroupId = groupId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int groupId, string title, string? description, string? content)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "Название и содержание урока обязательны");
                ViewBag.GroupId = groupId;
                return View();
            }

            var user = await GetCurrentUserAsync();

            var lesson = new Lesson
            {
                GroupId = groupId,
                Title = title.Trim(),
                Description = description?.Trim(),
                Content = content.Trim(),
                CreatedByUserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { groupId });
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var lesson = await _context.Lessons
                .Include(l => l.CreatedBy)
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return NotFound();
            if (!await IsGroupMemberOrOwnerAsync(lesson.GroupId))
                return Forbid();

            return View(lesson);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var user = await GetCurrentUserAsync();
            if (lesson.CreatedByUserId != user.Id && !await IsGroupOwnerAsync(lesson.GroupId))
                return Forbid();

            ViewBag.GroupId = lesson.GroupId;
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string? description, string? content)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var user = await GetCurrentUserAsync();
            if (lesson.CreatedByUserId != user.Id && !await IsGroupOwnerAsync(lesson.GroupId))
                return Forbid();

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "Название и содержание обязательны");
                ViewBag.GroupId = lesson.GroupId;
                return View(lesson);
            }

            lesson.Title = title.Trim();
            lesson.Description = description?.Trim();
            lesson.Content = content.Trim();
            lesson.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { groupId = lesson.GroupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var user = await GetCurrentUserAsync();
            var isCreator = lesson.CreatedByUserId == user.Id;
            var isGroupOwner = await IsGroupOwnerAsync(groupId);

            if (!isCreator && !isGroupOwner) return Forbid();

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { groupId });
        }
    }
}