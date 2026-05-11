using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models
{
    public class GroupMember
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int GroupId { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.Now;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    }
}
