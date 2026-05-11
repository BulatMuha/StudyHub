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

        public ChatHub(
            ApplicationDbContext context,
            ILogger<ChatHub> logger)
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

        public async Task SendMessage(int groupId, string text)
        {
            try
            {
                var userId = Context.UserIdentifier;

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Unauthorized send attempt");
                    await Clients.Caller.SendAsync("Error", "Не авторизован");
                    return;
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    await Clients.Caller.SendAsync("Error", "Сообщение не может быть пустым");
                    return;
                }

                if (text.Length > 5000)
                {
                    await Clients.Caller.SendAsync("Error", "Сообщение слишком длинное (макс. 5000 символов)");
                    return;
                }

                // Проверка участия в группе
                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (!isMember)
                {
                    _logger.LogWarning("User {UserId} not member of group {GroupId}", userId, groupId);
                    await Clients.Caller.SendAsync("Error", "Вы не участник этой группы");
                    return;
                }

                // Проверка статуса группы
                var group = await _context.Groups.FindAsync(groupId);
                if (group == null)
                {
                    await Clients.Caller.SendAsync("Error", "Группа не найдена");
                    return;
                }

                if (group.Status == Diplom_StudyHub.Models.Enums.GroupStatus.Archived)
                {
                    await Clients.Caller.SendAsync("Error", "Группа архивирована");
                    return;
                }

                // Создание сообщения
                var message = new Message
                {
                    GroupId = groupId,
                    SenderId = userId,
                    Text = text.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} created by {UserId} in group {GroupId}",
                    message.Id, userId, groupId);

                // Получение имени отправителя
                var user = await _context.Users.FindAsync(userId) as ApplicationUser;
                var senderName = user != null && !string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(user.FirstName)
                    ? $"{user.LastName} {user.FirstName}"
                    : user?.UserName ?? "Пользователь";

                // Отправка всем участникам группы
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendMessage");
                await Clients.Caller.SendAsync("Error", $"Ошибка сервера: {ex.Message}");
            }
        }

        public async Task JoinGroup(int groupId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (!isMember)
                {
                    _logger.LogWarning("User {UserId} tried to join non-member group {GroupId}", userId, groupId);
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, "Group_" + groupId);
                _logger.LogInformation("User {UserId} joined group {GroupId}", userId, groupId);

                await Clients.Caller.SendAsync("JoinedGroup", groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinGroup");
            }
        }

        public async Task LeaveGroup(int groupId)
        {
            try
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Group_" + groupId);
                _logger.LogInformation("User {UserId} left group {GroupId}", Context.UserIdentifier, groupId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LeaveGroup");
            }
        }

        public async Task LoadMessages(int groupId, int skip, int take)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    return;
                }

                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (!isMember)
                {
                    return;
                }

                if (take > 100) take = 100; // Защита от загрузки слишком большого количества

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
                    var senderName = sender != null && !string.IsNullOrEmpty(sender.LastName) && !string.IsNullOrEmpty(sender.FirstName)
                        ? $"{sender.LastName} {sender.FirstName}"
                        : m.Sender?.UserName ?? "Пользователь";

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in LoadMessages");
            }
        }

        public async Task DeleteMessage(int messageId)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId))
                {
                    await Clients.Caller.SendAsync("Error", "Не авторизован");
                    return;
                }

                var message = await _context.Messages
                    .Include(m => m.Group)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    await Clients.Caller.SendAsync("Error", "Сообщение не найдено");
                    return;
                }

                // Проверка прав: только автор или владелец группы
                var isGroupOwner = message.Group.OwnerId == userId;
                var isMessageAuthor = message.SenderId == userId;

                if (!isMessageAuthor && !isGroupOwner)
                {
                    await Clients.Caller.SendAsync("Error", "Недостаточно прав");
                    return;
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Message {MessageId} deleted by {UserId}", messageId, userId);

                // Уведомление всех участников
                await Clients.Group("Group_" + message.GroupId).SendAsync("MessageDeleted", messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteMessage");
                await Clients.Caller.SendAsync("Error", $"Ошибка: {ex.Message}");
            }
        }

        public async Task IsTyping(int groupId, bool isTyping)
        {
            try
            {
                var userId = Context.UserIdentifier;
                if (string.IsNullOrEmpty(userId)) return;

                var isMember = await _context.GroupMembers
                    .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);

                if (!isMember) return;

                var user = await _context.Users.FindAsync(userId) as ApplicationUser;
                var userName = user != null && !string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(user.FirstName)
                    ? $"{user.LastName} {user.FirstName}"
                    : user?.UserName ?? "Пользователь";

                await Clients.OthersInGroup("Group_" + groupId).SendAsync("UserTyping", new
                {
                    userId = userId,
                    userName = userName,
                    isTyping = isTyping
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in IsTyping");
            }
        }
    }
}