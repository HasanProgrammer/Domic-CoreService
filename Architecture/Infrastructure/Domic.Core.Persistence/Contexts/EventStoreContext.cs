using Domic.Core.Domain.Entities;
using Domic.Core.Persistence.Configs;
using Microsoft.EntityFrameworkCore;

namespace Domic.Core.Persistence.Contexts;

/*Setting*/
public partial class EventStoreContext(DbContextOptions<EventStoreContext> options) : DbContext(options);

/*Entity*/
public partial class EventStoreContext
{
    public DbSet<Event> EventStores { get; set; }
}

/*Config*/
public partial class EventStoreContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        
        base.OnConfiguring(optionsBuilder);
    }
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.ApplyConfiguration(new EventStoreConfig());
    }
}