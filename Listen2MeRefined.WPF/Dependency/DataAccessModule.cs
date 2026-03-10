using System.Data;
using Listen2MeRefined.Core.DomainObjects;
using Listen2MeRefined.Core.Models;
using Listen2MeRefined.Core.Repositories;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Listen2MeRefined.WPF.Dependency;

public static class DataAccessModule
{
    internal static IHostBuilder ConfigureDataAccess(this IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Register the factory — creates short-lived contexts on demand
            services.AddDbContextFactory<DataContext>(lifetime: ServiceLifetime.Singleton);

            // Register DataContext as transient for cases that inject it directly
            services.AddTransient<DataContext>(sp =>
                sp.GetRequiredService<IDbContextFactory<DataContext>>().CreateDbContext());

            services.AddSingleton<DbConnection>();

            services.AddSingleton<IDbConnection>(_ =>
            {
                var conn = new SqliteConnection(DbInfo.SqliteConnectionString);
                conn.Open();
                return conn;
            });

            services.AddSingleton<AudioRepository>();
            services.AddTransient<IAudioRepository>(ctx => ctx.GetRequiredService<AudioRepository>());
            services.AddTransient<IAdvancedDataReader<AdvancedFilter, AudioModel>>(ctx => ctx.GetRequiredService<AudioRepository>());
            services.AddTransient<IFromFolderRemover>(ctx => ctx.GetRequiredService<AudioRepository>());
            services.AddTransient<IRepository<AudioModel>>(ctx => ctx.GetRequiredService<AudioRepository>());
            
            services.AddTransient<IRepository<PlaylistModel>, PlaylistRepository>();
            services.AddTransient<IRepository<MusicFolderModel>, MusicFolderRepository>();
        });
        
        return builder;
    }
}