using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Diplom_StudyHub.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        public string UploaderId { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("GroupId")]
        public virtual Group Group { get; set; } = null!;

        [ForeignKey("UploaderId")]
        public virtual ApplicationUser Uploader { get; set; } = null!;
    }
}