using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class MessagesController : BaseController
    {
        public MessagesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index(int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var user = await GetCurrentUserAsync();

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(50)
                .ToListAsync();

            ViewBag.GroupId = groupId;
            ViewBag.CurrentUserId = user?.Id;

            return View(messages);
        }
    }
}