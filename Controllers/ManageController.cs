using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Diplom_StudyHub.Models;
using Diplom_StudyHub.Models.Enums;
using Diplom_StudyHub.Data;

namespace Diplom_StudyHub.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public ManageController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Email = user.Email;
            ViewBag.UserName = user.UserName;
            ViewBag.PhoneNumber = user.PhoneNumber;
            ViewBag.FirstName = user.FirstName;
            ViewBag.LastName = user.LastName;
            ViewBag.DateOfBirth = user.DateOfBirth?.ToString("yyyy-MM-dd");
            ViewBag.Gender = user.Gender;
            ViewBag.Age = user.Age;

            var userId = user.Id;
            ViewBag.GroupCount = await _context.GroupMembers.CountAsync(m => m.UserId == userId);

            ViewBag.LessonCount = await _context.Lessons
                .Include(l => l.Group)
                .ThenInclude(g => g.Members)
                .CountAsync(l => l.Group.Members.Any(m => m.UserId == userId));

            ViewBag.MessageCount = await _context.Messages
                .Include(m => m.Group)
                .ThenInclude(g => g.Members)
                .CountAsync(m => m.Group.Members.Any(mem => mem.UserId == userId));

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string firstName,
            string lastName,
            string phoneNumber,
            DateTime? dateOfBirth,
            Gender? gender)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(firstName) && firstName.Length > 50)
            {
                ModelState.AddModelError("firstName", "Имя не может быть длиннее 50 символов");
            }
            if (!string.IsNullOrWhiteSpace(lastName) && lastName.Length > 50)
            {
                ModelState.AddModelError("lastName", "Фамилия не может быть длиннее 50 символов");
            }
            if (dateOfBirth.HasValue && dateOfBirth.Value > DateTime.Today.AddYears(-10))
            {
                ModelState.AddModelError("dateOfBirth", "Минимальный возраст — 10 лет");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Email = user.Email;
                ViewBag.UserName = user.UserName;
                ViewBag.PhoneNumber = phoneNumber;
                ViewBag.FirstName = firstName;
                ViewBag.LastName = lastName;
                ViewBag.DateOfBirth = dateOfBirth?.ToString("yyyy-MM-dd");
                ViewBag.Gender = gender;
                return View("Index");
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;
            user.DateOfBirth = dateOfBirth;
            user.Gender = gender;

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Профиль обновлён";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Ошибка при обновлении профиля";
            return RedirectToAction(nameof(Index));
        }
    }
}