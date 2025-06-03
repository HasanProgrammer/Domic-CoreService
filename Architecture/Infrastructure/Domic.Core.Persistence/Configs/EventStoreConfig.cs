using Domic.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domic.Core.Persistence.Configs;

public class EventStoreConfig : IEntityTypeConfiguration<EventStore<string>>
{
    public void Configure(EntityTypeBuilder<EventStore<string>> builder)
    {
        builder.ToTable("EventStores");

        builder.HasKey(Event => Event.Id);
        
        /*-----------------------------------------------------------*/
        
        //Configs
    }
}