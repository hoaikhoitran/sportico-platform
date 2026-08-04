using Microsoft.EntityFrameworkCore;
using SporticoApp.Application.DTOs.Chat;
using SporticoApp.Application.Interfaces.Repositories;
using SporticoApp.Core.Entities;

namespace SporticoApp.Infrastructure.Persistence.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _context;

        public ChatRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom?> GetRoomByIdAsync(Guid roomId)
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == roomId);
        }

        public async Task<ChatRoom?> GetRoomByIdForUpdateAsync(Guid roomId)
        {
            return await _context.ChatRooms
                .FirstOrDefaultAsync(x => x.Id == roomId);
        }

        public async Task<ChatRoom?> GetRoomByUsersAsync(Guid userId1, Guid userId2)
        {
            var ordered1 = userId1.CompareTo(userId2) <= 0 ? userId1 : userId2;
            var ordered2 = userId1.CompareTo(userId2) <= 0 ? userId2 : userId1;

            return await _context.ChatRooms
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.User1Id == ordered1 && x.User2Id == ordered2);
        }

        public async Task<List<ChatRoom>> GetRoomsForUserAsync(Guid userId)
        {
            return await _context.ChatRooms
                .AsNoTracking()
                .Where(x => x.User1Id == userId || x.User2Id == userId)
                .OrderByDescending(x => x.LastMessageAt ?? x.CreatedAt)
                .ToListAsync();
        }

        public async Task<(List<Message> Items, int TotalCount)> GetMessagesByRoomAsync(
            Guid roomId,
            ChatMessageFilterRequest filter)
        {
            IQueryable<Message> query = _context.Messages
                .AsNoTracking()
                .Include(x => x.MessageAttachments)
                .Where(x => x.RoomId == roomId);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.SentAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<ChatRoom> AddRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Add(room);
            try
            {
                await _context.SaveChangesAsync();
                return room;
            }
            catch (DbUpdateException)
            {
                // A concurrent request created the room between our SELECT and INSERT.
                // Detach the failed entity and fall back to the existing row.
                _context.Entry(room).State = EntityState.Detached;

                var existing = await _context.ChatRooms
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.User1Id == room.User1Id && x.User2Id == room.User2Id);

                if (existing != null)
                    return existing;

                throw;
            }
        }

        public async Task AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
        }

        public Task AddMessageWithoutSaveAsync(Message message)
        {
            _context.Messages.Add(message);
            return Task.CompletedTask;
        }

        public Task AddAttachmentsWithoutSaveAsync(IEnumerable<MessageAttachment> attachments)
        {
            _context.MessageAttachments.AddRange(attachments);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
