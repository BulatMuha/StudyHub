using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Diplom_StudyHub.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public Diplom_StudyHub.Models.Enums.Gender? Gender { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int? Age
        {
            get
            {
                if (DateOfBirth == null)
                    return null;

                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Value.Year;

                if (DateOfBirth.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }

        // Навигационные свойства
        public virtual ICollection<Group> OwnedGroups { get; set; } = new HashSet<Group>();
        public virtual ICollection<GroupMember> GroupMemberships { get; set; } = new HashSet<GroupMember>();
        public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();
        public virtual ICollection<Document> UploadedDocuments { get; set; } = new HashSet<Document>();
        public virtual ICollection<Lesson> CreatedLessons { get; set; } = new HashSet<Lesson>();
    }
}