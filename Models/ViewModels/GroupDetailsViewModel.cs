using System;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class GroupDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public GroupStatus Status { get; set; }
        public string? AvatarUrl { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string? InviteCode { get; set; }
        public int MemberCount { get; set; }
        public bool IsOwner { get; set; }
    }
}
