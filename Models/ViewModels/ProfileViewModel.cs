using System;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public int OwnedGroupsCount { get; set; }
        public int MemberGroupsCount { get; set; }
        public int MessagesCount { get; set; }
        public string MemberSince { get; set; } = string.Empty;
    }
}