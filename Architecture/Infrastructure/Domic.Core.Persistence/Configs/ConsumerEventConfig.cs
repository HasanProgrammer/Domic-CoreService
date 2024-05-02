using Domic.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domic.Core.Persistence.Configs;

public class ConsumerEventConfig : IEntityTypeConfiguration<ConsumerEvent>
{
    public void Configure(EntityTypeBuilder<ConsumerEvent> builder)
    {
        builder.ToTable("ConsumerEvents");

        builder.HasKey(Event => Event.Id);
        
        /*-----------------------------------------------------------*/
        
        //Configs
        
        builder.Property(entity => entity.CreatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.CreatedAt_PersianDate).IsRequired();
    }
}