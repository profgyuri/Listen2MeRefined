using System.Data;
using Autofac;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Data.Dapper;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Data.Repositories;
using Microsoft.Data.Sqlite;

namespace Listen2MeRefined.WPF.Dependency.Modules;

public class DataAccessModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder
            .RegisterType<DataContext>()
            .SingleInstance();

        builder
            .RegisterType<DbConnection>()
            .SingleInstance();

        builder
            .Register(_ =>
            {
                var conn = new SqliteConnection(DbInfo.SqliteConnectionString);
                conn.Open();
                return conn;
            })
            .As<IDbConnection>()
            .SingleInstance();

        builder
            .RegisterType<AudioRepository>()
            .As<IRepository<AudioModel>>()
            .As<IAdvancedDataReader<ParameterizedQuery, AudioModel>>();

        builder
            .RegisterType<PlaylistRepository>()
            .As<IRepository<PlaylistModel>>();

        builder
            .RegisterType<MusicFolderRepository>()
            .As<IRepository<MusicFolderModel>>();
    }
}