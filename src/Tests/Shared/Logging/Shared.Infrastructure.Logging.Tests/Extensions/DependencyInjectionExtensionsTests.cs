using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Application.Core.Configuration.Extensions;
using Shared.Common.Helpers;
using Shared.Infrastructure.Logging.Extensions;
using Shared.Testing.Configuration;

namespace Shared.Infrastructure.Logging.Tests.Extensions;

public sealed class DependencyInjectionExtensionsTests
{
    [Fact]
    public void AddNlog_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var config = TestConfigurationBuilder.Empty();

        var result = services.AddNlog(config);

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddNlog_RegistersLoggingServices()
    {
        var services = new ServiceCollection();
        var config = CreateConfigWithNlogPath();

        services.AddNlog(config);
        var provider = services.BuildServiceProvider();

        var loggerFactory = provider.GetService<ILoggerFactory>();
        loggerFactory.Should().NotBeNull();
    }

    [Fact]
    public void AddNlog_WithAbsolutePathInSettings_DoesNotThrow()
    {
        var tempConfig = CreateTempNlogConfig();
        try
        {
            var moduleName = AssemblyHelper.GetModuleName();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{moduleName}:NlogSettings:Path"] = tempConfig,
                    [$"{moduleName}:NlogSettings:LogLevel"] = "Information"
                })
                .Build();
            var services = new ServiceCollection();

            var act = () => services.AddNlog(config);

            act.Should().NotThrow();
        }
        finally
        {
            File.Delete(tempConfig);
        }
    }

    [Fact]
    public void AddNlog_WithDefaultConfig_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var config = TestConfigurationBuilder.Empty();

        var act = () => services.AddNlog(config);

        act.Should().NotThrow();
    }

    private static IConfiguration CreateConfigWithNlogPath()
    {
        var tempConfig = CreateTempNlogConfig();
        try
        {
            var moduleName = AssemblyHelper.GetModuleName();
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"{moduleName}:NlogSettings:Path"] = tempConfig,
                    [$"{moduleName}:NlogSettings:LogLevel"] = "Information"
                })
                .Build();
        }
        catch
        {
            File.Delete(tempConfig);
            throw;
        }
    }

    private static string CreateTempNlogConfig()
    {
        var path = Path.GetTempFileName();
        var xml = """
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="false"
      internalLogLevel="Off">
  <extensions>
    <add assembly="NLog.Web.AspNetCore"/>
  </extensions>
  <targets>
    <target name="console" xsi:type="Console"
            layout="${longdate} | ${level:uppercase=true} | ${logger} | ${message:withexception=true}"/>
  </targets>
  <rules>
    <logger name="*" minlevel="Info" writeTo="console"/>
  </rules>
</nlog>
""";
        File.WriteAllText(path, xml);
        return path;
    }
}
