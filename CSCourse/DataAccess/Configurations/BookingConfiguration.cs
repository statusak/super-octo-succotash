using CSCourse.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCourse.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            //builder.HasOne(o => o.User)
            //       .WithMany(u => u.Orders)
            //       .HasForeignKey(o => o.UserId)
            //       .OnDelete();
        }
    }
}
