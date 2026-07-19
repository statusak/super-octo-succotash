using CSCourse.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CSCourse.Infrastructure.DataAccess.Configurations
{
    public class AccountConfiguration : IEntityTypeConfiguration<Account>
    {
        public void Configure(EntityTypeBuilder<Account> builder)
        {
            builder.ToTable("accounts");
            // https://stackoverflow.com/q/47013752
            builder.Property(e => e.Id).ValueGeneratedNever();
            builder.HasKey(e => e.Id);

            builder.Property(a => a.Login)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(a => a.Login).IsUnique();
                
            builder.Property(a => a.HashPassword)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(a => a.Role)
                .IsRequired();
        }
    }
}
