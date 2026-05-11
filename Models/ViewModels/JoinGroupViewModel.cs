using System;
using System.ComponentModel.DataAnnotations;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class JoinGroupViewModel
    {
        [Required(ErrorMessage = "Код приглашения обязателен")]
        [Display(Name = "Код приглашения")]
        public string InviteCode { get; set; } = string.Empty;
    }

    public class GroupMemberViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public GroupMemberRole Role { get; set; }
        public DateTime JoinedAt { get; set; }
    }
}
