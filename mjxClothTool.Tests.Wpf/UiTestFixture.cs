using System.Text.Json;

namespace mjxClothTool.Tests.Wpf;

public sealed class UiTestFixture : IDisposable
{
    private readonly string _settingsPath;

    public UiTestFixture()
    {
        TestRoot = Path.Combine(Path.GetTempPath(), "mjxClothTool-ui-tests", Guid.NewGuid().ToString("N"));
        ProjectsRoot = Path.Combine(TestRoot, "projects");
        LocalAppDataRoot = Path.Combine(TestRoot, "local-app-data");

        Directory.CreateDirectory(ProjectsRoot);
        Directory.CreateDirectory(LocalAppDataRoot);

        _settingsPath = Path.Combine(LocalAppDataRoot, "mjxClothTool", "settings.json");
    }

    public string TestRoot { get; }

    public string ProjectsRoot { get; }

    public string LocalAppDataRoot { get; }

    public FlaUiTestApplication LaunchApplication()
    {
        WriteCleanSettings();
        return FlaUiTestApplication.Launch(LocalAppDataRoot);
    }

    public string GetProjectPath(string projectName)
    {
        return Path.Combine(ProjectsRoot, projectName);
    }

    public void Dispose()
    {
        if (Directory.Exists(TestRoot))
        {
            Directory.Delete(TestRoot, recursive: true);
        }
    }

    private void WriteCleanSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);

        var settings = new
        {
            IsFirstRun = false,
            MainProjectsFolder = ProjectsRoot,
            RecentlyOpenedProjects = Array.Empty<object>()
        };

        File.WriteAllText(
            _settingsPath,
            JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}
