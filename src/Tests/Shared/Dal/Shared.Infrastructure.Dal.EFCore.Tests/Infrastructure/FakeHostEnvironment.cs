using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

internal sealed class FakeHostEnvironment
    : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "Shared.Infrastructure.Dal.EFCore.Tests";

    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
