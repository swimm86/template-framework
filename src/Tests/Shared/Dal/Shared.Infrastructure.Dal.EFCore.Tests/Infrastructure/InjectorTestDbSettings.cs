using Shared.Infrastructure.Dal.EFCore.Settings;

namespace Shared.Infrastructure.Dal.EFCore.Tests.Infrastructure;

public sealed class InjectorTestDbSettings
    : EfDbSettingsBase<InjectorTestDbContext>;
