using Diplom_StudyHub.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Diplom_StudyHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        [PersonalData]
        [StringLength(50, ErrorMessage = "Имя не может быть длиннее 50 символов")]
        [Display(Name = "Имя")]
        public string? FirstName { get; set; }

        [PersonalData]
        [StringLength(50, ErrorMessage = "Фамилия не может быть длиннее 50 символов")]
        [Display(Name = "Фамилия")]
        public string? LastName { get; set; }

        [PersonalData]
        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [PersonalData]
        [Display(Name = "Пол")]
        public Gender? Gender { get; set; }

        [PersonalData]
        [Display(Name = "Телефон")]
        public override string? PhoneNumber
        {
            get => base.PhoneNumber;
            set => base.PhoneNumber = value;
        }

        [NotMapped]
        public int? Age
        {
            get
            {
                if (DateOfBirth.HasValue)
                {
                    var today = DateTime.Today;
                    var age = today.Year - DateOfBirth.Value.Year;
                    if (DateOfBirth.Value.Date > today.AddYears(-age)) age--;
                    return age;
                }
                return null;
            }
        }

        [NotMapped]
        public string FullName => $"{LastName} {FirstName}".Trim();
    }
}