using System.ComponentModel.DataAnnotations;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class CreateGroupViewModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        [StringLength(100, ErrorMessage = "Название не может быть длиннее 100 символов")]
        [Display(Name = "Название группы")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Описание не может быть длиннее 500 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [StringLength(256)]
        [Display(Name = "Аватарка (URL)")]
        public string? AvatarUrl { get; set; }
    }
}