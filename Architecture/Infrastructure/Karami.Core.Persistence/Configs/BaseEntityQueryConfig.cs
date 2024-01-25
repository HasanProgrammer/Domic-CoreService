using Karami.Core.Domain.Contracts.Abstracts;
using Karami.Core.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Karami.Core.Persistence.Configs;

public class BaseEntityQueryConfig<TEntityQuery, TKey> : IEntityTypeConfiguration<TEntityQuery> 
    where TEntityQuery : EntityQuery<TKey>
{
    public virtual void Configure(EntityTypeBuilder<TEntityQuery> builder)
    {
        builder.HasIndex(entity => new { entity.Id , entity.IsDeleted });
        
        builder.HasKey(entity => entity.Id);
        
        builder.Property(entity => entity.IsActive)
               .HasConversion(new EnumToNumberConverter<IsActive, byte>())
               .IsRequired();
        
        builder.Property(entity => entity.IsDeleted)
               .HasConversion(new EnumToNumberConverter<IsDeleted, byte>())
               .IsRequired();
        
        builder.Property(entity => entity.CreatedBy)  .IsRequired();
        builder.Property(entity => entity.CreatedRole).IsRequired();
        
        builder.Property(entity => entity.CreatedAt_EnglishDate).IsRequired();
        builder.Property(entity => entity.CreatedAt_PersianDate).IsRequired();
        
        //Optimestic solution for concurrency
        builder.Property(entity => entity.Version).IsConcurrencyToken().IsRequired();

        builder.HasQueryFilter(entity => entity.IsDeleted == IsDeleted.UnDelete);
    }
}