using System.ComponentModel.DataAnnotations;

namespace Diplom_StudyHub.Models.Enums
{
    public enum Gender
    {
        [Display(Name = "Не указан")]
        NotSpecified = 0,

        [Display(Name = "Мужской")]
        Male = 1,

        [Display(Name = "Женский")]
        Female = 2
    }
}