using CSCourse.DataAccess;
using CSCourse.Models;
using Microsoft.EntityFrameworkCore;

namespace CSCourse.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> CreateAsync(Event @event)
    {
        @event.Id = Guid.NewGuid();
        _context.Events.Add(@event);

        try
        {
            await _context.SaveChangesAsync();
            return @event.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(@event).State = EntityState.Detached;
            return await CreateAsync(@event);
        }
    }
}