using Karami.Core.Domain.Entities;
using Karami.Core.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Karami.Core.Persistence.Configs;

public class EventConfig : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(Event => Event.Id);
        
        /*-----------------------------------------------------------*/
        
        builder.Property(vo => vo.CreatedAt_EnglishDate).IsRequired();
        builder.Property(vo => vo.CreatedAt_PersianDate).IsRequired();
        builder.Property(vo => vo.UpdatedAt_EnglishDate).IsRequired();
        builder.Property(vo => vo.UpdatedAt_PersianDate).IsRequired();

        builder.Property(Event => Event.IsActive)
               .HasConversion(new EnumToNumberConverter<IsActive , int>())
               .IsRequired();
    }
}