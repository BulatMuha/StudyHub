using Diplom_StudyHub.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Diplom_StudyHub.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Diplom_StudyHub.Models.Group> Groups { get; set; } = default!;
        public DbSet<Diplom_StudyHub.Models.GroupMember> GroupMembers { get; set; }
        public DbSet<Diplom_StudyHub.Models.Message> Messages { get; set; }
        public DbSet<Diplom_StudyHub.Models.Document> Documents { get; set; }
        public DbSet<Diplom_StudyHub.Models.Lesson> Lessons { get; set; }
        public DbSet<Diplom_StudyHub.Models.Notification> Notifications { get; set; }

        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Document>()
                .HasOne(d => d.Group)
                .WithMany(g => g.Documents)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Lesson>()
                .HasOne(l => l.Group)
                .WithMany(g => g.Lessons)
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<GroupMember>()
                .HasOne(gm => gm.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .Property(n => n.Title)
                .IsUnicode(true);

            builder.Entity<Notification>()
                .Property(n => n.Message)
                .IsUnicode(true);

            base.OnModelCreating(builder);
        }
    }
}
