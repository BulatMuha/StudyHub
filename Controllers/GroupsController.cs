using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.ViewModels;
using Diplom_StudyHub.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class GroupsController : BaseController
    {
        public GroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index(int page = 1, string searchQuery = "", string statusFilter = "")
        {
            int pageSize = 12;
            var user = await GetCurrentUserAsync();
            var userId = user?.Id;

            var query = _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchQuery))
            {
                query = query.Where(g => g.Name.Contains(searchQuery) ||
                                        (g.Description != null && g.Description.Contains(searchQuery)));
            }

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<GroupStatus>(statusFilter, out var status))
            {
                query = query.Where(g => g.Status == status);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var groups = await query
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModels = groups.Select(g => new GroupListViewModel
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

            ViewBag.Pagination = new PaginationViewModel
            {
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                ItemsPerPage = pageSize
            };

            ViewBag.SearchQuery = searchQuery;
            ViewBag.StatusFilter = statusFilter;

            return View(viewModels);
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.Groups
                .Include(g => g.Owner)
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            var user = await GetCurrentUserAsync();

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
            if (!ModelState.IsValid) return View(model);

            var user = await GetCurrentUserAsync();
            if (user == null) return NotFound();

            var newGroup = new Group
            {
                Name = model.Name,
                Description = model.Description,
                AvatarUrl = model.AvatarUrl,
                Status = GroupStatus.Open,
                CreatedAt = DateTime.UtcNow,
                OwnerId = user.Id,
                InviteCode = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()
            };

            _context.Groups.Add(newGroup);
            await _context.SaveChangesAsync();

            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = newGroup.Id,
                UserId = user.Id,
                Role = GroupMemberRole.Owner,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            if (!await IsGroupOwnerAsync(group.Id)) return Forbid();

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Group group)
        {
            if (id != group.Id) return NotFound();

            var existingGroup = await _context.Groups.FindAsync(id);
            if (existingGroup == null) return NotFound();

            if (!await IsGroupOwnerAsync(id)) return Forbid();

            if (ModelState.IsValid)
            {
                existingGroup.Name = group.Name;
                existingGroup.Description = group.Description;
                existingGroup.AvatarUrl = group.AvatarUrl;
                existingGroup.Status = group.Status;

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(group);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();
            if (!await IsGroupOwnerAsync(group.Id)) return Forbid();

            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var group = await _context.Groups.FindAsync(id);
            if (group != null && await IsGroupOwnerAsync(id))
            {
                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Join(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return NotFound();

            if (await IsGroupMemberAsync(id))
                return RedirectToAction(nameof(Index));

            var group = await _context.Groups.FindAsync(id);
            if (group == null) return NotFound();

            _context.GroupMembers.Add(new GroupMember
            {
                GroupId = id,
                UserId = user.Id,
                Role = GroupMemberRole.Member,
                JoinedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Leave(int id)
        {
            var user = await GetCurrentUserAsync();
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
    }
}