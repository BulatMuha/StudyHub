using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
using Diplom_StudyHub.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class GroupMembersController : BaseController
    {
        public GroupMembersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public IActionResult Join() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Join(JoinGroupViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var group = await _context.Groups.FirstOrDefaultAsync(g => g.InviteCode == model.InviteCode);
            if (group == null)
            {
                ModelState.AddModelError("", "Группа с таким кодом приглашения не найдена");
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            var existingMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == group.Id && m.UserId == user.Id);

            if (existingMember)
            {
                ModelState.AddModelError("", "Вы уже являетесь участником этой группы");
                return View(model);
            }

            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = group.Id,
                UserId = user.Id,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Groups", new { id = group.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Leave(int groupId)
        {
            var user = await GetCurrentUserAsync();
            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == user.Id);

            if (member == null) return NotFound();
            if (member.Role == GroupMemberRole.Owner)
            {
                ModelState.AddModelError("", "Владелец не может покинуть группу");
                return RedirectToAction("Details", "Groups", new { id = groupId });
            }

            _context.GroupMembers.Remove(member);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Groups");
        }

        public async Task<IActionResult> Index(int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var members = await _context.GroupMembers
                .Include(m => m.User)
                .Where(m => m.GroupId == groupId)
                .Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    UserName = m.User != null ? (m.User.UserName ?? "Неизвестно") : "Неизвестно",
                    Email = m.User != null ? (m.User.Email ?? "") : "",
                    FirstName = m.User != null ? m.User.FirstName : null,
                    LastName = m.User != null ? m.User.LastName : null,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.IsOwner = await IsGroupOwnerAsync(groupId);

            return View(members);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int groupId, string userId)
        {
            if (!await IsGroupOwnerAsync(groupId))
                return Forbid();

            var currentUser = await GetCurrentUserAsync();
            if (userId == currentUser.Id)
            {
                ModelState.AddModelError("", "Нельзя удалить себя");
                return RedirectToAction(nameof(Index), new { groupId });
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (member != null)
            {
                _context.GroupMembers.Remove(member);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { groupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int groupId, string userId, GroupMemberRole newRole)
        {
            if (!await IsGroupOwnerAsync(groupId))
                return Forbid();

            if (newRole == GroupMemberRole.Owner)
            {
                ModelState.AddModelError("", "Нельзя назначить роль Владельца");
                return RedirectToAction(nameof(Index), new { groupId });
            }

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (member != null)
            {
                member.Role = newRole;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index), new { groupId });
        }
    }
}