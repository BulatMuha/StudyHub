using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models.ViewModels
{
    public class GroupListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public GroupStatus Status { get; set; }
        public string? OwnerName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOwner { get; set; }
        public bool IsMember { get; set; }
    }
}