using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class FakeHostEnvironment(string? environmentName = null)
    : IHostEnvironment
{
    public string EnvironmentName { get; set; } = environmentName ?? Environments.Development;

    public string ApplicationName { get; set; } = "Shared.Infrastructure.Dal.EFCore.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
