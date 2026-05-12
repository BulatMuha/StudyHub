using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class ManageProfileViewModel
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public class InputModel
        {
            [StringLength(50, ErrorMessage = "Имя не может быть длиннее 50 символов")]
            [Display(Name = "Имя")]
            public string? FirstName { get; set; }

            [StringLength(50, ErrorMessage = "Фамилия не может быть длиннее 50 символов")]
            [Display(Name = "Фамилия")]
            public string? LastName { get; set; }

            [Phone]
            [Display(Name = "Телефон")]
            public string? PhoneNumber { get; set; }

            [Display(Name = "Дата рождения")]
            [DataType(DataType.Date)]
            public DateTime? DateOfBirth { get; set; }

            [Display(Name = "Пол")]
            public Diplom_StudyHub.Models.Enums.Gender? Gender { get; set; }
        }
    }
}