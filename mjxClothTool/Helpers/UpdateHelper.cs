using System.Diagnostics;
using System.Reflection;

namespace mjxClothTool.Helpers;

/// <summary>
/// Provides local version information. Online update checks are disabled.
/// </summary>
public static class UpdateHelper
{
    public static string GetCurrentVersion()
    {
        string assemblyLocation = Assembly.GetEntryAssembly()?.Location;
        string fileVersion = string.IsNullOrWhiteSpace(assemblyLocation)
            ? null
            : FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;

        return fileVersion ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown";
    }

}
