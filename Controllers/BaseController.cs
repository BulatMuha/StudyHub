using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;

        public BaseController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        protected async Task<ApplicationUser> GetCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return user ?? throw new InvalidOperationException("Пользователь не найден");
        }

        protected async Task<bool> IsGroupOwnerAsync(int groupId)
        {
            var user = await GetCurrentUserAsync();
            return await _context.Groups.AnyAsync(g => g.Id == groupId && g.OwnerId == user.Id);
        }

        protected async Task<bool> IsGroupMemberAsync(int groupId)
        {
            var user = await GetCurrentUserAsync();
            return await _context.GroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == user.Id);
        }

        protected async Task<bool> IsGroupMemberOrOwnerAsync(int groupId)
        {
            return await IsGroupOwnerAsync(groupId) || await IsGroupMemberAsync(groupId);
        }
    }
}