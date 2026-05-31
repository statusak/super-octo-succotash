using CSCourse.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCourse.DataAccess.Configurations
{
    public class EventConfiguration : IEntityTypeConfiguration<Event>
    {
        public void Configure(EntityTypeBuilder<Event> builder)
        {
            builder.Metadata.SetTableName("Events");
            // https://stackoverflow.com/q/47013752
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Title).IsRequired().HasMaxLength(64);
            builder.Property(e => e.Description).HasMaxLength(512);
            
            builder.Property(e => e.TotalSeats).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(e => e.AvailableSeats).IsRequired().HasColumnType("decimal(18,2)");

            builder.Property(e => e.StartAt).IsRequired().HasColumnType("TIMESTAMP");
            builder.Property(e => e.EndAt).IsRequired().HasColumnType("TIMESTAMP");

            builder.HasMany(e => e.Bookings).WithOne(b => b.Event).HasForeignKey(b => b.EventId);
        }
    }
}
