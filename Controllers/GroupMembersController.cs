using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
using Diplom_StudyHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class GroupMembersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public GroupMembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Join()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinGroupViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.InviteCode == model.InviteCode);

            if (group == null)
            {
                ModelState.AddModelError("", "Группа с таким кодом не найдена");
                return View(model);
            }

            // ✅ Проверка: группа не архивирована
            if (group.Status == GroupStatus.Archived)
            {
                ModelState.AddModelError("", "Эта группа архивирована");
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var existingMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == user.Id);

            if (existingMember)
            {
                ModelState.AddModelError("", "Вы уже являетесь участником этой группы");
                return View(model);
            }

            var newMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.Now,
            };

            _context.GroupMembers.Add(newMember);
            await _context.SaveChangesAsync();

            // ✅ TODO: Создать уведомление для владельца группы
            // await CreateNotificationAsync(group.OwnerId, $"Новый участник: {user.UserName}", "info", group.Id);

            return RedirectToAction("Details", "Groups", new { id = group.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == user.Id);

            if (member == null)
            {
                return NotFound();
            }

            if (member.Role == GroupMemberRole.Owner)
            {
                ModelState.AddModelError("", "Владелец не может покинуть группу");
                return RedirectToAction("Details", "Groups", new { id = groupId });
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Groups");
        }

        public async Task<IActionResult> Index(int? groupId)
        {
            if (groupId == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            // ✅ ПРОВЕРКА: пользователь должен быть участником группы
            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember)
            {
                return Forbid();
            }

            var members = await _context.GroupMembers
                .Include(m => m.User)
                .Where(m => m.GroupId == groupId)
                .Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    UserName = m.User != null ? (m.User.UserName ?? "Неизвестно") : "Неизвестно",
                    Email = m.User != null ? (m.User.Email ?? "") : "",
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.IsOwner = user != null && await _context.Groups.AnyAsync(g => g.Id == groupId && g.OwnerId == user.Id);

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int groupId, string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            if (userId == currentUser.Id)
            {
                ModelState.AddModelError("", "Нельзя удалить владельца");
                return RedirectToAction("Index", new { groupId });
            }

            var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (member != null)
            {
                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();

                // ✅ TODO: Создать уведомление для удалённого участника
                // await CreateNotificationAsync(userId, $"Вы удалены из группы: {group.Name}", "warning", groupId);
            }

            return RedirectToAction("Index", new { groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int groupId, string userId, GroupMemberRole newRole)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return NotFound();
            }

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null || group.OwnerId != currentUser.Id)
            {
                return Forbid();
            }

            // ✅ Нельзя изменить роль владельца
            if (newRole == GroupMemberRole.Owner)
            {
                ModelState.AddModelError("", "Нельзя назначить роль владельца");
                return RedirectToAction("Index", new { groupId });
            }

            var member = await _context.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
            if (member != null)
            {
                member.Role = newRole;
                await _context.SaveChangesAsync();

                // ✅ TODO: Создать уведомление для участника
                // await CreateNotificationAsync(userId, $"Ваша роль изменена на: {newRole}", "info", groupId);
            }

            return RedirectToAction("Index", new { groupId });
        }
    }
}