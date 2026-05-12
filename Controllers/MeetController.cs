using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    public class MeetController : BaseController
    {
        public MeetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
            : base(context, userManager)
        {
        }

        public async Task<IActionResult> Index(int groupId)
        {
            if (!await IsGroupMemberOrOwnerAsync(groupId))
                return Forbid();

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
                return NotFound();

            var user = await GetCurrentUserAsync();

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.UserName = !string.IsNullOrEmpty(user?.FirstName) && !string.IsNullOrEmpty(user?.LastName)
                ? $"{user.LastName} {user.FirstName}"
                : user?.UserName ?? "Пользователь";
            ViewBag.Email = user?.Email;
            ViewBag.RoomName = $"StudyHub-Group-{groupId}";

            return View();
        }
    }
}