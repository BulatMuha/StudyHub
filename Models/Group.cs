using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
using Diplom_StudyHub.Models.Enums;

namespace Diplom_StudyHub.Models

{
    public class Group
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Название группы")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public GroupStatus Status { get; set; } = GroupStatus.Open;  

        [StringLength(256)]
        [Display(Name = "Аватарка(URL)")]
        public string? AvatarUrl { get; set; }

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [ForeignKey("OwnerId")]
        public ApplicationUser? Owner { get; set; }

        [StringLength(50)]
        [Display(Name = "Код приглашения")]
        public string? InviteCode { get; set; }
        public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();  
    }
}
