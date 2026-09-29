using System.Text.Json;

namespace mjxClothTool.Tests.Wpf;

/// <summary>
/// Launches the application once for the lifetime of a test class and resets
/// to the home screen between tests. Use with <see cref="IClassFixture{SharedAppFixture}"/>
/// on any test class that needs a live app but wants to avoid the per-test startup cost.
/// </summary>
public sealed class SharedAppFixture : IDisposable
{
    private readonly string _testRoot;
    private readonly string _settingsPath;

    public SharedAppFixture()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "mjxClothTool-ui-tests", Guid.NewGuid().ToString("N"));
        ProjectsRoot = Path.Combine(_testRoot, "projects");
        var localAppDataRoot = Path.Combine(_testRoot, "local-app-data");

        Directory.CreateDirectory(ProjectsRoot);
        Directory.CreateDirectory(localAppDataRoot);

        _settingsPath = Path.Combine(localAppDataRoot, "mjxClothTool", "settings.json");
        WriteCleanSettings();

        App = FlaUiTestApplication.Launch(localAppDataRoot);
    }

    public FlaUiTestApplication App { get; }

    public string ProjectsRoot { get; }

    public void ResetToHome()
    {
        // Close any modal dialog that may still be open (e.g. test failed mid-flow)
        foreach (var modal in App.MainWindow.ModalWindows)
        {
            try { modal.Close(); } catch { /* best-effort */ }
        }

        App.NavigateHome();
    }

    public void Dispose()
    {
        App.Dispose();

        if (Directory.Exists(_testRoot))
        {
            try { Directory.Delete(_testRoot, recursive: true); } catch { /* best-effort */ }
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
