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

    public List<Event> GetFilteredPage(FilterRepositoryEventDto filterEvent, int page, int pageSize)
    {
        var filteredEvents = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(filterEvent.Title))
        {
            filteredEvents = filteredEvents.Where(e =>
                EF.Functions.ILike(e.Title, $"%{filterEvent.Title}%"));
        }

        if (filterEvent.StartAt != null)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= filterEvent.StartAt);
        }

        if (filterEvent.EndAt != null)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= filterEvent.EndAt);
        }
        
        return filteredEvents.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }
    public async Task<List<Event>> GetFilteredPageAsync(FilterRepositoryEventDto filterEvent, int page, int pageSize)
    {
        var filteredEvents = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(filterEvent.Title))
        {
            filteredEvents = filteredEvents.Where(e =>
                EF.Functions.ILike(e.Title, $"%{filterEvent.Title}%"));
        }

        if (filterEvent.StartAt != null)
        {
            filteredEvents = filteredEvents.Where(e => e.StartAt >= filterEvent.StartAt);
        }

        if (filterEvent.EndAt != null)
        {
            filteredEvents = filteredEvents.Where(e => e.EndAt <= filterEvent.EndAt);
        }

        return await filteredEvents.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }


    public List<Event> GetPage(int page, int pageSize)
    {
        return _context.Events.Skip((page - 1) * pageSize).Take(pageSize).ToList();
    }
    public async Task<List<Event>> GetPageAsync(int page, int pageSize)
    {
        return await _context.Events
                .AsQueryable()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
    }
    public int Count()
    {
        return _context.Events.Count();
    }
    public async Task<int> CountAsync()
    {
        return await _context.Events.CountAsync();
    }
}