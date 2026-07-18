using CSCourse.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCourse.Infrastructure.DataAccess.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");
            // https://stackoverflow.com/q/47013752
            builder.Property(b => b.Id).ValueGeneratedNever();
            builder.HasKey(b => b.Id);

            builder.Property(b => b.EventId).IsRequired();

            builder.Property(b => b.UserId).IsRequired();

            builder.Property(b => b.Status).IsRequired().HasConversion<string>();

            builder.Property(b => b.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
            builder.Property(b => b.ProcessedAt).HasColumnType("timestamp with time zone");

            builder.HasOne(e => e.Event).WithMany(b => b.Bookings).HasForeignKey(b => b.EventId);
            builder.HasOne(b => b.Account).WithMany().HasForeignKey(b => b.UserId);
        }
    }
}
