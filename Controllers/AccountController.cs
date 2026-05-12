using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var model = await BuildProfileViewModel(user);
            return View(model);
        }

        public async Task<IActionResult> UserProfile(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var model = await BuildProfileViewModel(user);
            return View("Profile", model);
        }

        private async Task<ProfileViewModel> BuildProfileViewModel(ApplicationUser user)
        {
            var ownedGroups = await _context.Groups.CountAsync(g => g.OwnerId == user.Id);
            var memberGroups = await _context.GroupMembers.CountAsync(m => m.UserId == user.Id);
            var messages = await _context.Messages.CountAsync(m => m.SenderId == user.Id);

            var displayName = !string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(user.LastName)
                ? $"{user.LastName} {user.FirstName}"
                : user.UserName ?? "Пользователь";

            return new ProfileViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? "",
                Email = user.Email ?? "",
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                OwnedGroupsCount = ownedGroups,
                MemberGroupsCount = memberGroups,
                MessagesCount = messages,
                MemberSince = user.CreatedAt.ToString("dd.MM.yyyy")
            };
        }
    }
}