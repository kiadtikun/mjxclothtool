using System;

namespace mjxClothTool.Helpers;

public static class AppDataHelper
{
    private const string LocalAppDataOverrideVariable = "MJXCLOTHTOOL_LOCALAPPDATA";

    public static string GetLocalAppDataPath()
    {
        var overridePath = Environment.GetEnvironmentVariable(LocalAppDataOverrideVariable);

        return string.IsNullOrWhiteSpace(overridePath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : overridePath;
    }
}
