using Karami.Core.Domain.Contracts.Abstracts;
using Karami.Core.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Karami.Core.Persistence.Configs;

public class BaseEntityConfig<TEntity, TKey> : IEntityTypeConfiguration<TEntity> where TEntity : Entity<TKey>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
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
        
        //Optimestic solution for concurrency
        builder.Property(entity => entity.Version).IsConcurrencyToken().IsRequired();
        
        builder.OwnsOne(entity => entity.CreatedAt, createdAt => {
            createdAt.Property(vo => vo.EnglishDate).IsRequired().HasColumnName("CreatedAt_EnglishDate");
            createdAt.Property(vo => vo.PersianDate).IsRequired().HasColumnName("CreatedAt_PersianDate");
        })
        .Navigation(entity => entity.CreatedAt)
        .IsRequired();
        
        builder.OwnsOne(entity => entity.UpdatedAt, updatedAt => {
            updatedAt.Property(vo => vo.EnglishDate).HasColumnName("UpdatedAt_EnglishDate");
            updatedAt.Property(vo => vo.PersianDate).HasColumnName("UpdatedAt_PersianDate");
        });

        builder.HasQueryFilter(entity => entity.IsDeleted == IsDeleted.UnDelete);
    }
}