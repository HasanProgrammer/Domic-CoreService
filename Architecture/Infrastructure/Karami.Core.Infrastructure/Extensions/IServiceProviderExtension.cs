using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Karami.Core.Infrastructure.Extensions;

public static class IServiceProviderExtension
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <typeparam name="TContext"></typeparam>
    public static void AutoMigration<TContext>(this IServiceProvider serviceProvider, Action<TContext> seeder = null) 
        where TContext : DbContext
    {
        //Trigger
        using IServiceScope scope = serviceProvider.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        if (context.Database.GetPendingMigrations().Any())
        {
            context.Database.Migrate();
            
            //Seeder data
            seeder?.Invoke(context);
        }
    }
}