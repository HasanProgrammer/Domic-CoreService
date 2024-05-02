using Domic.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domic.Core.Persistence.Configs;

public class ConsumerEventQueryConfig : IEntityTypeConfiguration<ConsumerEventQuery>
{
    public void Configure(EntityTypeBuilder<ConsumerEventQuery> builder)
    {
        builder.ToTable("ConsumerEvents");

        builder.HasKey(Event => Event.Id);
        
        /*-----------------------------------------------------------*/
        
        //Configs
        
        builder.Ignore(entity => entity.CreatedBy);
        builder.Ignore(entity => entity.CreatedRole);
        builder.Ignore(entity => entity.UpdatedBy);
        builder.Ignore(entity => entity.UpdatedRole);
        builder.Ignore(entity => entity.IsDeleted);
        builder.Ignore(entity => entity.Version);
        
        builder.Property(entity => entity.CreatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.CreatedAt_PersianDate).IsRequired();
    }
}