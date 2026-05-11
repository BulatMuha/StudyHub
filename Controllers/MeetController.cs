using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class MeetController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MeetController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }
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

            var group = await _context.Groups.FindAsync(groupId);
            if (group == null)
            {
                return NotFound();
            }

            ViewBag.GroupId = groupId;
            ViewBag.GroupName = group.Name;
            ViewBag.UserName = user.UserName;
            ViewBag.Email = user.Email;
            ViewBag.RoomName = $"StudyHub-Group-{groupId}";

            return View();
        }
    }
}
