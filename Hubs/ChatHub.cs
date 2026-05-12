using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Diplom_StudyHub.Data;
using Diplom_StudyHub.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Diplom_StudyHub.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ApplicationDbContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("User connected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, Context.UserIdentifier);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("User disconnected: {ConnectionId}, User: {UserId}",
                Context.ConnectionId, Context.UserIdentifier);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(int groupId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (isMember)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Group_" + groupId);
            }
        }

        public async Task SendMessage(int groupId, string text)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(text))
            {
                await Clients.Caller.SendAsync("Error", "Сообщение не может быть пустым");
                return;
            }

            text = text.Trim();
            if (text.Length > 5000) text = text.Substring(0, 5000);

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (!isMember)
            {
                await Clients.Caller.SendAsync("Error", "Вы не участник этой группы");
                return;
            }

            var message = new Message
            {
                GroupId = groupId,
                SenderId = userId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId) as ApplicationUser;
            var senderName = user != null
                ? (!string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(user.FirstName)
                    ? $"{user.LastName} {user.FirstName}"
                    : user.UserName ?? "Пользователь")
                : "Пользователь";

            var messageData = new
            {
                id = message.Id,
                text = message.Text,
                createdAt = message.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                senderId = userId,
                senderName = senderName
            };

            await Clients.Group("Group_" + groupId).SendAsync("ReceiveMessage", messageData);
        }

        public async Task LoadMessages(int groupId, int skip, int take = 50)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            if (take > 100) take = 100;

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.GroupId == groupId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            var result = messages.Select(m =>
            {
                var sender = m.Sender as ApplicationUser;
                var senderName = sender != null
                    ? (!string.IsNullOrEmpty(sender.LastName) && !string.IsNullOrEmpty(sender.FirstName)
                        ? $"{sender.LastName} {sender.FirstName}"
                        : sender.UserName ?? "Пользователь")
                    : "Пользователь";

                return new
                {
                    id = m.Id,
                    text = m.Text,
                    createdAt = m.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                    senderId = m.SenderId,
                    senderName = senderName
                };
            });

            await Clients.Caller.SendAsync("ReceiveMessages", result);
        }

        public async Task DeleteMessage(int messageId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var message = await _context.Messages
                .Include(m => m.Group)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null) return;

            var isGroupOwner = message.Group.OwnerId == userId;
            var isMessageAuthor = message.SenderId == userId;

            if (!isGroupOwner && !isMessageAuthor) return;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            await Clients.Group("Group_" + message.GroupId).SendAsync("MessageDeleted", messageId);
        }

        public async Task IsTyping(int groupId, bool isTyping)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;

            var isMember = await _context.GroupMembers
                .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

            if (!isMember) return;

            var user = await _context.Users.FindAsync(userId) as ApplicationUser;
            var userName = user != null
                ? (!string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(user.FirstName)
                    ? $"{user.LastName} {user.FirstName}"
                    : user.UserName ?? "Пользователь")
                : "Пользователь";

            await Clients.OthersInGroup("Group_" + groupId).SendAsync("UserTyping", new
            {
                userId = userId,
                userName = userName,
                isTyping = isTyping
            });
        }
    }
}