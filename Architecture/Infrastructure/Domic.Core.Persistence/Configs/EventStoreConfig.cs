using Domic.Core.Domain.Entities;
using Domic.Core.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Domic.Core.Persistence.Configs;

public class EventStoreConfig : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("EventStores");

        builder.HasKey(Event => Event.Id);
        
        /*-----------------------------------------------------------*/
        
        //Configs

        builder.Ignore(entity => entity.Table);
        builder.Ignore(entity => entity.CreatedBy);
        builder.Ignore(entity => entity.CreatedRole);
        builder.Ignore(entity => entity.UpdatedBy);
        builder.Ignore(entity => entity.UpdatedRole);
        builder.Ignore(entity => entity.IsDeleted);
        builder.Ignore(entity => entity.UpdatedAt_EnglishDate);
        builder.Ignore(entity => entity.UpdatedAt_PersianDate);
        
        builder.Property(entity => entity.CreatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.CreatedAt_PersianDate).IsRequired();

        builder.Property(Event => Event.IsActive)
               .HasConversion(new EnumToNumberConverter<IsActive, int>())
               .IsRequired();
    }
}