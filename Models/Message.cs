using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Diplom_StudyHub.Models
{
    public class Message
    {
        public int Id { get; set; }

        [Required]
        [StringLength(1000)]
        public string Text { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public int GroupId { get; set; }

        [ForeignKey("GroupId")]
        public Group? Group { get; set; } 
        public string? Title { get; set; }

        [ForeignKey("SenderId")]
        public ApplicationUser? Sender { get; set; }

    }
}
