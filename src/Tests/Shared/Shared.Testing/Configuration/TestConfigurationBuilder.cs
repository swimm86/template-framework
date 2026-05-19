using Microsoft.Extensions.Configuration;

namespace Shared.Testing.Configuration;

public static class TestConfigurationBuilder
{
    public static IConfiguration Empty() => new ConfigurationBuilder().Build();

    public static IConfiguration WithSettings(IDictionary<string, string?> settings)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }

    public static IConfiguration WithJson(string json)
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);
        try
        {
            return new ConfigurationBuilder()
                .AddJsonFile(path, optional: false)
                .Build();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
