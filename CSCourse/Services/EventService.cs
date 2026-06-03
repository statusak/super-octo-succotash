using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CSCourse.Services
{
    public class EventService : IEventService
    {
        private readonly AppDbContext _context;
        private readonly IEventRepository _events;

        private readonly object _lockCreateEvent = new object();

        private readonly SemaphoreSlim _processingSemaphoreEvent = new(1, 1);

        public EventService(AppDbContext context, IEventRepository events)
        {
            _context = context;
            _events = events;
        }

        public PaginatedResult GetAll(int page, int pageSize)
        {
            return new PaginatedResult
            {
                CountEvents = _events.Count(),
                Events = _events.GetPage(page, pageSize)
            }; 
        }

        public async Task<PaginatedResult> GetAllAsync(int page, int pageSize)
        {
            return new PaginatedResult
            {
                CountEvents = await _events.CountAsync(),
                Events = await _events.GetPageAsync(page, pageSize)
            };

        }

        public PaginatedResult GetAll(FilterEvent filterEvent, int page, int pageSize)
        {
            FilterRepositoryEventDto filterRepositoryEventDto = new FilterRepositoryEventDto
            {
                Title = filterEvent.Title,
                StartAt = filterEvent.StartAt,
                EndAt = filterEvent.EndAt,
            };

            return new PaginatedResult
            {
                CountEvents = _events.Count(),
                Events = _events.GetFilteredPage(filterRepositoryEventDto, page, pageSize)
            };
        }

        public async Task<PaginatedResult> GetAllAsync(FilterEvent filterEvent, int page, int pageSize)
        {

            FilterRepositoryEventDto filterRepositoryEventDto = new FilterRepositoryEventDto
            {
                Title = filterEvent.Title,
                StartAt = filterEvent.StartAt,
                EndAt = filterEvent.EndAt,
            };
           
            return new PaginatedResult
            {
                CountEvents = await _events.CountAsync(),
                Events = await _events.GetFilteredPageAsync(filterRepositoryEventDto, page, pageSize)
            };
        }

        public Event? GetEventById(Guid id)
        {
            return _context.Events.First(x => x.Id == id);
        }

        public async Task<Event?> GetEventByIdAsync(Guid id)
        {
            return await _context.Events.FirstAsync(x => x.Id == id);
        }

        public bool IsEventExists(Guid id)
        {
            return _context.Events.Any(x => x.Id == id);
        }

        public async Task<bool> IsEventExistsAsync(Guid id)
        {
            return await _context.Events.AnyAsync(x => x.Id == id);
        }

        public bool TryReserveSeats(Guid id, int count = 1)
        {
            lock (_lockCreateEvent)
            {
                var @event = _context.Events.First(x => x.Id == id);
                if (@event.AvailableSeats - count < 0) {
                    return false;
                }
                @event.AvailableSeats -= count;
                _context.SaveChanges();
                return true;
            }
        }

        public async Task<bool> TryReserveSeatsAsync(Guid id, int count = 1)
        {
            await _processingSemaphoreEvent.WaitAsync();
            try
            {
                var @event = await _context.Events.FirstAsync(x => x.Id == id);
                if (@event.AvailableSeats - count < 0)
                {
                    return false;
                }
                @event.AvailableSeats -= count;
                await _context.SaveChangesAsync();
                return true;
            }
            finally
            {
                _processingSemaphoreEvent.Release();
            }
        }

        public bool ReleaseSeats(Guid id, int count = 1)
        {
            lock (_lockCreateEvent)
            {
                var @event = _context.Events.First(x => x.Id == id);
                if (@event.AvailableSeats + count > @event.TotalSeats)
                {
                    return false;
                }
                @event.AvailableSeats += count;
                _context.SaveChanges();
                return true;
            }
        }

        public async Task<bool> ReleaseSeatsAsync(Guid id, int count = 1)
        {
            await _processingSemaphoreEvent.WaitAsync();
            try
            {
                var @event = await _context.Events.FirstAsync(x => x.Id == id);
                if (@event.AvailableSeats + count > @event.TotalSeats)
                {
                    return false;
                }
                @event.AvailableSeats += count;
                await _context.SaveChangesAsync();
                return true;
            }
            finally
            {
                _processingSemaphoreEvent.Release();
            }
        }

        public Guid CreateEvent(Event @event)
        {
            if (@event.TotalSeats <= 0)
            {
                throw new ValidationException("@event.TotalSeats <= 0");
            }

            Guid eventId;

            lock (_lockCreateEvent)
            {
                do
                {
                    eventId = Guid.NewGuid();
                }
                while (_context.Events.Any(e => e.Id == eventId));

                @event.Id = eventId;
                _context.Events.Add(@event);
                _context.SaveChanges();
            }

            return eventId;
        }

        public async Task<Guid> CreateEventAsync(Event @event)
        {
            if (@event.TotalSeats <= 0)
            {
                throw new ValidationException("@event.TotalSeats <= 0");
            }

            return await _events.CreateAsync(@event);
        }

        public bool UpdateEvent(Guid id, Event @event)
        {
            var @event_old = _context.Events.First(x => x.Id == id);
            if (@event_old != null)
            {
                @event_old.Title = @event.Title;
                @event_old.Description = @event.Description;
                @event_old.StartAt = @event.StartAt;
                @event_old.EndAt = @event.EndAt;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateEventAsync(Guid id, Event @event)
        {
            var rowsAffected = await _context.Events
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Title, e => @event.Title)
                    .SetProperty(e => e.Description, e => @event.Description)
                    .SetProperty(e => e.StartAt, e => @event.StartAt)
                    .SetProperty(e => e.EndAt, e => @event.EndAt)
            );

            return rowsAffected > 0;

        }

        public bool UpdateEvent(Guid id, string Title, string? Description, DateTime StartAt, DateTime EndAt)
        {
            var @event_old = _context.Events.First(x => x.Id == id);
            if (@event_old != null)
            {
                @event_old.Title = Title;
                @event_old.Description = Description;
                @event_old.StartAt = StartAt;
                @event_old.EndAt = EndAt;
                _context.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateEventAsync(Guid id, string Title, string? Description, DateTime StartAt, DateTime EndAt)
        {
            var rowsAffected = await _context.Events
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Title, e => Title)
                    .SetProperty(e => e.Description, e => Description)
                    .SetProperty(e => e.StartAt, e => StartAt)
                    .SetProperty(e => e.EndAt, e => EndAt)
            );

            return rowsAffected > 0;
        }
        public void DeleteEvent(Guid id)
        {
            var @event = _context.Events.First(x => x.Id == id);
            if (@event != null) {
                _context.Events.Remove(@event);
                _context.SaveChanges();
            }
        }

        public async Task DeleteEventAsync(Guid id)
        {
            var @event = await _context.Events.FirstAsync(x => x.Id == id);
            if (@event != null)
            {
                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();
            }
        }
    }
}
