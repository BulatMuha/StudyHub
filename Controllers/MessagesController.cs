using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
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
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ ЗАГРУЖАЕМ ТОЛЬКО ПОСЛЕДНИЕ 50 СООБЩЕНИЙ (остальные через SignalR lazy loading)
        public async Task<IActionResult> Index(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var isMember = await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
            if (!isMember)
            {
                return Forbid();
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.CurrentUserId = user.Id;

            return View(messages);
        }

        // ✅ УДАЛЁНО: Create и Delete методы больше не нужны (используем SignalR)
        // Оставлены только для совместимости если SignalR не работает
    }
}