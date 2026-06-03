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
            return _events.GetById(id);
        }

        public async Task<Event?> GetEventByIdAsync(Guid id)
        {
            return await _events.GetByIdAsync(id);
        }

        public bool IsEventExists(Guid id)
        {
            return _events.IsExists(id);
        }

        public async Task<bool> IsEventExistsAsync(Guid id)
        {
            return await _events.IsExistsAsync(id);
        }

        public bool TryReserveSeats(Guid id, int count = 1)
        {
            lock (_lockCreateEvent)
            {
                return _events.TryReserveSeats(id, count);
            }
        }

        public async Task<bool> TryReserveSeatsAsync(Guid id, int count = 1)
        {
            await _processingSemaphoreEvent.WaitAsync();
            try
            {
                return await _events.TryReserveSeatsAsync(id, count);
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
                return _events.TryReleaseSeats(id, count);
            }
        }

        public async Task<bool> ReleaseSeatsAsync(Guid id, int count = 1)
        {
            await _processingSemaphoreEvent.WaitAsync();
            try
            {
                return await _events.TryReleaseSeatsAsync(id, count);
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

            return _events.Create(@event);
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
            var eventRepositoryUpdateDto = new EventRepositoryUpdateDto
            {
                Id = id,
                Title = @event.Title,
                Description = @event.Description,
                StartAt = @event.StartAt,
                EndAt = @event.EndAt,
            };
            return _events.Update(eventRepositoryUpdateDto);
        }

        public async Task<bool> UpdateEventAsync(Guid id, Event @event)
        {
            var eventRepositoryUpdateDto = new EventRepositoryUpdateDto
            {
                Id = id,
                Title = @event.Title,
                Description = @event.Description,
                StartAt = @event.StartAt,
                EndAt = @event.EndAt,
            };
            return await _events.UpdateAsync(eventRepositoryUpdateDto);
        }

        public bool UpdateEvent(Guid id, string Title, string? Description, DateTime StartAt, DateTime EndAt)
        {
            var eventRepositoryUpdateDto = new EventRepositoryUpdateDto
            {
                Id = id,
                Title = Title,
                Description = Description,
                StartAt = StartAt,
                EndAt = EndAt,
            };
            return _events.Update(eventRepositoryUpdateDto);
        }

        public async Task<bool> UpdateEventAsync(Guid id, string Title, string? Description, DateTime StartAt, DateTime EndAt)
        {
            var eventRepositoryUpdateDto = new EventRepositoryUpdateDto
            {
                Id = id,
                Title = Title,
                Description = Description,
                StartAt = StartAt,
                EndAt = EndAt,
            };
            return await _events.UpdateAsync(eventRepositoryUpdateDto);
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
