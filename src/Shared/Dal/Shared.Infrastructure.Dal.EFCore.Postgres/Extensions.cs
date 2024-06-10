using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shared.Application.Core.Configuration;
using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Postgres;

public static class Extensions
{
    public static IServiceCollection AddPostgresDbContext<T>(this IServiceCollection serviceCollection,
        IConfiguration configuration,
        string migrationAssemblyName,
        Action<DbContextOptionsBuilder>? optionsAction = default)
        where T : DbContext
    {
        var settings = configuration.GetOptions<DbSettings>()!;
        var sourceBuilder = new NpgsqlDataSourceBuilder(settings.ConnectionString);
        return serviceCollection.AddDbContextFactory<T>(opt =>
        {
            opt.UseNpgsql(sourceBuilder.Build(), builder => builder.MigrationsAssembly(migrationAssemblyName));
            optionsAction?.Invoke(opt);
        });
    }
}
