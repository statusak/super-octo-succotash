using CSCourse.DataAccess;
using CSCourse.Interfaces;
using CSCourse.Models;
using Microsoft.EntityFrameworkCore;

namespace CSCourse.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }
}