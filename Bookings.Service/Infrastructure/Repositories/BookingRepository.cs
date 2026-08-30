using Bookings.Service.Domain.Models;
using Bookings.Service.Application.Interfaces;
using Bookings.Service.Application.Models;
using Bookings.Service.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Service.Infrastructure.Repositories;

/// <summary>
/// Репозиторий для работы с бронированиями через Entity Framework Core.
/// Реализует интерфейс <see cref="IBookingRepository"/>.
/// </summary>
public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Инициализирует новый экземпляр <see cref="BookingRepository"/> с указанным контекстом базы данных.
    /// </summary>
    /// <param name="context">Контекст базы данных <see cref="AppDbContext"/>.</param>
    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создаёт новое бронирование в базе данных (синхронно).
    /// При конфликте (DbUpdateException) отсоединяет сущность и повторяет попытку.
    /// </summary>
    /// <param name="booking">DTO с данными для создания бронирования.</param>
    /// <returns>Идентификатор созданного бронирования.</returns>
    public Guid Create(BookingRepositoryCreateDto booking)
    {
        var newBooking = new Booking {
            Id = Guid.NewGuid(),
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        _context.Bookings.Add(newBooking);

        try
        {
            _context.SaveChanges();
            return newBooking.Id;
        }
        catch (DbUpdateException)
        {
            _context.Entry(newBooking).State = EntityState.Detached;
            return Create(booking);
        }
    }

    /// <summary>
    /// Создаёт новое бронирование в базе данных асинхронно.
    /// При конфликте (DbUpdateException) отсоединяет сущность и повторяет попытку.
    /// </summary>
    /// <param name="booking">DTO с данными для создания бронирования.</param>
    /// <returns>Созданная сущность <see cref="Booking"/>.</returns>
    public async Task<Booking> CreateAsync(BookingRepositoryCreateDto booking)
    {
        var newBooking = new Booking {
            Id = Guid.NewGuid(),
            EventId = booking.EventId,
            UserId = booking.UserId,
            Status = booking.Status,
            CreatedAt = booking.CreatedAt,
            ProcessedAt = booking.ProcessedAt
        };

        _context.Bookings.Add(newBooking);

        try
        {
            await _context.SaveChangesAsync();
            return newBooking;
        }
        catch (DbUpdateException)
        {
            _context.Entry(newBooking).State = EntityState.Detached;
            return await CreateAsync(booking);
        }
    }

    /// <summary>
    /// Возвращает все бронирования со статусом <see cref="BookingStatus.Pending"/> (синхронно).
    /// </summary>
    /// <returns>Перечисление бронирований в статусе Pending.</returns>
    public IEnumerable<Booking> GetPending()
    {
        return _context.Bookings.Where(x => x.Status == BookingStatus.Pending);
    }

    /// <summary>
    /// Возвращает все бронирования со статусом <see cref="BookingStatus.Pending"/> асинхронно.
    /// </summary>
    /// <returns>Список бронирований в статусе Pending.</returns>
    public async Task<IEnumerable<Booking>> GetPendingAsync()
    {
        return await _context.Bookings.Where(x => x.Status == BookingStatus.Pending).ToListAsync();
    }

    /// <summary>
    /// Возвращает бронирование по идентификатору асинхронно.
    /// </summary>
    /// <param name="id">Идентификатор бронирования.</param>
    /// <returns>Сущность <see cref="Booking"/> или null, если не найдена.</returns>
    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);
    }

    /// <summary>
    /// Обновляет статус и время обработки бронирования по идентификатору (без загрузки сущности в контекст).
    /// </summary>
    /// <param name="booking">DTO с обновлёнными данными бронирования.</param>
    /// <returns>true, если строка была обновлена; иначе false.</returns>
    public async Task<bool> UpdateAsync(BookingRepositoryUpdateDto booking)
    {
        var rowsAffected = await _context.Bookings
            .Where(x => x.Id == booking.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, b => booking.Status)
                .SetProperty(b => b.ProcessedAt, b => booking.ProcessedAt)
        );

        return rowsAffected > 0;
    }

    /// <summary>
    /// Подсчитывает количество активных бронирований (Pending или Confirmed) пользователя
    /// по заданному списку идентификаторов мероприятий.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="eventIds">Коллекция идентификаторов мероприятий.</param>
    /// <returns>Количество активных бронирований.</returns>
    public async Task<int> GetCountActiveBookingsByUserAndEventIdsAsync(Guid userId, IEnumerable<Guid> eventIds)
    {
        if (!eventIds.Any())
                return 0;

        var activeStatuses = new[] { BookingStatus.Pending, BookingStatus.Confirmed };

        return await _context.Bookings
            .Where(b => b.UserId == userId
                    && activeStatuses.Contains(b.Status)
                    && eventIds.Contains(b.EventId))
            .CountAsync();
    }
}
