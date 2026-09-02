using Foodeez.Application.Interfaces.Repositories;
using Foodeez.Domain.Entities;
using Foodeez.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Foodeez.Infrastructure.Repositories;

public class ChatRepository : BaseRepository<ChatConversation>, IChatRepository
{
    public ChatRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ChatConversationSummary>> GetSummariesAsync(Guid userId)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new ChatConversationSummary(
                c.Id,
                c.Title,
                c.LastMessageAt,
                c.CreatedAt,
                c.Messages.Count,
                c.Messages
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => m.Content)
                    .FirstOrDefault()))
            .ToListAsync();
    }

    public async Task<ChatConversation?> GetWithMessagesAsync(Guid conversationId)
    {
        return await _dbSet
            .Where(c => c.Id == conversationId)
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync();
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        // Same reason as meal plan entries: the id is assigned in the constructor, so adding
        // to the parent collection alone reads as an existing row and issues a no-op UPDATE.
        await _context.Set<ChatMessage>().AddAsync(message);
    }

    public async Task<ChatMessage?> GetMessageAsync(Guid messageId)
    {
        return await _context.Set<ChatMessage>()
            .Include(m => m.Conversation)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }
}
