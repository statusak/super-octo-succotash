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

    public Guid Create(Event @event)
    {
        @event.Id = Guid.NewGuid();
        _context.Events.Add(@event);

        try
        {
            _context.SaveChanges();
            return @event.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(@event).State = EntityState.Detached;
            return Create(@event);
        }
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
            // TODO: Сделать такую логику, чтобы ошибка не зацикливалась.
            //       Иначе это может привести к вечному циклу и зависанию
            _context.Entry(@event).State = EntityState.Detached;
            return await CreateAsync(@event);
        }
    }

    public Event GetById(Guid id)
    {
        return _context.Events.First(x => x.Id == id);
    }
    public async Task<Event> GetByIdAsync(Guid id)
    {
        return await _context.Events.FirstAsync(x => x.Id == id);
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
    public bool IsExists(Guid id)
    {
        return _context.Events.Any(x => x.Id == id);
    }
    public async Task<bool> IsExistsAsync(Guid id)
    {
        return await _context.Events.AnyAsync(x => x.Id == id);
    }

    public async Task<bool> TryReserveSeatsAsync(Guid id, int count)
    {
        // System.Data.IsolationLevel.RepeatableRead needed for using MVCC for prevent Phantom RW 
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var @event = await _context.Events.FirstAsync(e => e.Id == id);
            if (@event.AvailableSeats < count)
            {
                // RollbackAsync need for release line in DB
                await transaction.RollbackAsync();
                return false;
            }

            @event.AvailableSeats -= count;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public bool TryReserveSeats(Guid id, int count)
    {
        // System.Data.IsolationLevel.RepeatableRead needed for using MVCC for prevent Phantom RW 
        using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var @event = _context.Events.First(e => e.Id == id);
            if (@event.AvailableSeats < count)
            {
                // Rollback need for release line in DB
                transaction.Rollback();
                return false;
            }

            @event.AvailableSeats -= count;
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> TryReleaseSeatsAsync(Guid id, int count)
    {
        // System.Data.IsolationLevel.RepeatableRead needed for using MVCC for prevent Phantom RW 
        using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var @event = await _context.Events.FirstAsync(e => e.Id == id);
            if (@event.AvailableSeats + count > @event.TotalSeats)
            {
                // RollbackAsync need for release line in DB
                await transaction.RollbackAsync();
                return false;
            }

            @event.AvailableSeats += count;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public bool TryReleaseSeats(Guid id, int count)
    {
        // System.Data.IsolationLevel.RepeatableRead needed for using MVCC for prevent Phantom RW 
        using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var @event = _context.Events.First(e => e.Id == id);
            if (@event.AvailableSeats + count > @event.TotalSeats)
            {
                // Rollback need for release line in DB
                transaction.Rollback();
                return false;
            }

            @event.AvailableSeats += count;
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public bool Update(EventRepositoryUpdateDto @event)
    {
        // System.Data.IsolationLevel.RepeatableRead needed for using MVCC for prevent Phantom RW 
        using var transaction = _context.Database.BeginTransaction(System.Data.IsolationLevel.RepeatableRead);
        try
        {
            var @event_old = _context.Events.First(e => e.Id == @event.Id);
            if (@event_old != null)
            {
                @event_old.Title = @event.Title;
                @event_old.Description = @event.Description;
                @event_old.StartAt = @event.StartAt;
                @event_old.EndAt = @event.EndAt;
                _context.SaveChanges();
                transaction.Commit();
                return true;
            }
            // Rollback need for release line in DB
            transaction.Rollback();
            return false;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> UpdateAsync(EventRepositoryUpdateDto @event)
    {
        var rowsAffected = await _context.Events
            .Where(x => x.Id == @event.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Title, e => @event.Title)
                .SetProperty(e => e.Description, e => @event.Description)
                .SetProperty(e => e.StartAt, e => @event.StartAt)
                .SetProperty(e => e.EndAt, e => @event.EndAt)
        );

        return rowsAffected > 0;
    }

    public bool Delete(Guid id)
    {
        var @event = _context.Events.First(e => e.Id == id);
        if (@event != null)
        {
            _context.Events.Remove(@event);
            _context.SaveChanges();
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var @event = await _context.Events.FirstAsync(e => e.Id == id);
        if (@event != null)
        {
            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
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