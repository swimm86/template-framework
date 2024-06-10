using System.Reflection;

namespace Shared.Common;

public static class Helpers
{
    public static string GetModuleName() => Assembly.GetEntryAssembly()!.GetName().Name!;
}
