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
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ ПАГИНАЦИЯ (10 групп на страницу)
        // ✅ ПАГИНАЦИЯ + ПРОВЕРКА ПРАВ (10 групп на страницу)
        public async Task<IActionResult> Index(int page = 1)
        {
            int pageSize = 10;

            var groups = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id;

            var groupViewModels = groups.Select(g => new GroupListViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CreatedAt = g.CreatedAt,
                Status = g.Status,
                OwnerName = g.Owner?.UserName ?? "Неизвестно",
                AvatarUrl = g.AvatarUrl,
                IsOwner = userId != null && g.OwnerId == userId,
                IsMember = userId != null && g.Members.Any(m => m.UserId == userId)
            }).ToList();

            var totalItems = groupViewModels.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paginatedGroups = groupViewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                ItemsPerPage = pageSize
            };

            return View(paginatedGroups);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (group == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);

            var viewModel = new GroupDetailsViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CreatedAt = group.CreatedAt,
                Status = group.Status,
                AvatarUrl = group.AvatarUrl,
                OwnerName = group.Owner?.UserName ?? "Неизвестно",
                InviteCode = (user != null && group.OwnerId == user.Id) ? group.InviteCode : null,
                MemberCount = group.Members?.Count ?? 0,
                IsOwner = user != null && group.OwnerId == user.Id
            };

            return View(viewModel);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            // ✅ ОТЛАДКА: выводим ошибки валидации
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Validation Error: {error.ErrorMessage}");
                }
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return NotFound();

                try
                {
                    var newgroup = new Group
                    {
                        Name = model.Name,
                        Description = model.Description,
                        AvatarUrl = model.AvatarUrl,
                        Status = GroupStatus.Open,
                        CreatedAt = DateTime.Now,
                        OwnerId = user.Id,
                        InviteCode = Guid.NewGuid().ToString().Substring(0, 8)
                    };

                    _context.Add(newgroup);
                    await _context.SaveChangesAsync();

                    _context.GroupMembers.Add(new GroupMember
                    {
                        GroupId = newgroup.Id,
                        UserId = user.Id,
                        Role = GroupMemberRole.Owner,
                        JoinedAt = DateTime.Now
                    });
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Exception: {ex.Message}");
                    ModelState.AddModelError("", $"Ошибка: {ex.Message}");
                }
            }
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || group.OwnerId != user.Id) return Forbid();

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Status,AvatarUrl")] Group group)
        {
            if (id != group.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var existingGroup = await _context.Groups.FindAsync(id);

            if (user == null || existingGroup == null || existingGroup.OwnerId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    existingGroup.Name = group.Name;
                    existingGroup.Description = group.Description;
                    existingGroup.Status = group.Status;
                    existingGroup.AvatarUrl = group.AvatarUrl;

                    _context.Update(existingGroup);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GroupExists(group.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(group);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var group = await _context.Groups.Include(g => g.Owner).FirstOrDefaultAsync(m => m.Id == id);
            if (group == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || group.OwnerId != user.Id) return Forbid();

            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group != null)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || group.OwnerId != user.Id) return Forbid();

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
        // ✅ Вступить в группу
        public async Task<IActionResult> Join(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            // Проверка: уже участник?
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == user.Id);

            if (existingMember != null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Добавляем участника
            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = id,
                UserId = user.Id,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ Покинуть группу
        public async Task<IActionResult> Leave(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == user.Id);

            if (member != null && member.Role != GroupMemberRole.Owner)
            {
                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ✅ Вступить по коду (для закрытых групп) — ПЕРЕАДРЕСАЦИЯ НА ФОРМУ ВВОДА
        public async Task<IActionResult> JoinByCode(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            // Проверка: уже участник?
            var existingMember = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == user.Id);

            if (existingMember != null)
            {
                return RedirectToAction(nameof(Index));
            }

            // Передаём groupId во ViewBag и показываем форму ввода кода
            // Предполагаем что есть View: Views/GroupMembers/Join.cshtml
            ViewBag.GroupId = id;
            ViewBag.GroupName = group.Name;
            return View("~/Views/GroupMembers/Join.cshtml");
        }
        private bool GroupExists(int id) => _context.Groups.Any(e => e.Id == id);
    }
}