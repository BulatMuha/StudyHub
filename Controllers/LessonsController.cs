using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Diplom_StudyHub.Models.ViewModels;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class LessonsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public LessonsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ ПАГИНАЦИЯ (15 уроков на страницу)
        public async Task<IActionResult> Index(int groupId, int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null) return NotFound();

            int pageSize = 15;

            var lessons = await _context.Lessons
                .Include(l => l.CreatedBy)
                .Where(l => l.GroupId == groupId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            var totalItems = lessons.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paginatedLessons = lessons
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

            return View(paginatedLessons);
        }

        public IActionResult Create(int groupId)
        {
            ViewBag.GroupId = groupId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int groupId, string title, string description, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.Status == GroupStatus.Archived)
            {
                ModelState.AddModelError("", "Нельзя создавать уроки в архивированной группе");
                ViewBag.GroupId = groupId;
                return View();
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Название урока обязательно");
                ViewBag.GroupId = groupId;
                return View();
            }

            if (title.Length > 200)
            {
                ModelState.AddModelError("", "Название урока не должно превышать 200 символов");
                ViewBag.GroupId = groupId;
                return View();
            }

            try
            {
                var lesson = new Lesson
                {
                    GroupId = groupId,
                    Title = title,
                    Description = description,
                    Content = content,
                    CreatedByUserId = user.Id,
                    CreatedAt = DateTime.Now
                };

                _context.Lessons.Add(lesson);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { groupId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при создании урока: {ex.Message}");
                ViewBag.GroupId = groupId;
                return View();
            }
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var lesson = await _context.Lessons
                .Include(l => l.CreatedBy)
                .Include(l => l.Group)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (lesson == null) return NotFound();

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == lesson.GroupId && m.UserId == user.Id);
            if (!isMember) return Forbid();

            return View(lesson);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || lesson.CreatedByUserId != user.Id) return Forbid();

            ViewBag.GroupId = lesson.GroupId;
            return View(lesson);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string title, string description, string content)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            if (lesson.CreatedByUserId != user.Id) return Forbid();

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError("", "Название урока обязательно");
                ViewBag.GroupId = lesson.GroupId;
                return View(lesson);
            }

            try
            {
                lesson.Title = title;
                lesson.Description = description;
                lesson.Content = content;
                lesson.UpdatedAt = DateTime.Now;

                _context.Update(lesson);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", new { groupId = lesson.GroupId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при обновлении: {ex.Message}");
                ViewBag.GroupId = lesson.GroupId;
                return View(lesson);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return NotFound();

            var isCreator = lesson.CreatedByUserId == user.Id;
            var isGroupOwner = await _context.Groups.AnyAsync(g => g.Id == groupId && g.OwnerId == user.Id);

            if (!isCreator && !isGroupOwner) return Forbid();

            try
            {
                _context.Lessons.Remove(lesson);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Ошибка при удалении: {ex.Message}");
            }

            return RedirectToAction("Index", new { groupId });
        }

        private bool LessonExists(int id)
        {
            return _context.Lessons.Any(e => e.Id == id);
        }
    }
}