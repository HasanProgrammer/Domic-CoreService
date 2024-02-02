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
        
        //Configs

        builder.Ignore(entity => entity.CreatedBy);
        builder.Ignore(entity => entity.CreatedRole);
        builder.Ignore(entity => entity.UpdatedBy);
        builder.Ignore(entity => entity.UpdatedRole);
        
        builder.Property(entity => entity.CreatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.CreatedAt_PersianDate).IsRequired();
        builder.Property(entity => entity.UpdatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.UpdatedAt_PersianDate).IsRequired();

        builder.Property(Event => Event.IsActive)
               .HasConversion(new EnumToNumberConverter<IsActive, int>())
               .IsRequired();
    }
}