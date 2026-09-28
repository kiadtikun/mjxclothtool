using grzyClothTool.Extensions;
using grzyClothTool.Properties;
using grzyClothTool.Views;
using Material.Icons;
using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using static grzyClothTool.Controls.CustomMessageBox;

namespace grzyClothTool;

/// <summary>
/// Interaction logic for App.xaml. The application intentionally operates offline.
/// </summary>
public partial class App : Application
{
    public static ISplashScreen splashScreen;
    private ManualResetEvent ResetSplashCreated;
    private Thread SplashThread;

    protected override void OnStartup(StartupEventArgs e)
    {
        ResetSplashCreated = new ManualResetEvent(false);

        SplashThread = new Thread(ShowSplash);
        SplashThread.SetApartmentState(ApartmentState.STA);
        SplashThread.Start();

        ResetSplashCreated.WaitOne();
        base.OnStartup(e);

        ChangeTheme(Settings.Default.IsDarkMode);
    }

    public App()
    {
        MaterialIconDataProvider.Instance = new CustomIconProvider();
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
    }

    private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = (Exception)e.ExceptionObject;

        Show($"An error occurred: {ex.Message}", "Error", CustomMessageBoxButtons.OKOnly);
        WriteLocalErrorLog(ex);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteLocalErrorLog(e.Exception);
        e.Handled = true;
    }

    private static void WriteLocalErrorLog(Exception exception)
    {
        try
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var path = Path.Combine(AppContext.BaseDirectory, $"error-{date}.log");
            File.WriteAllText(path, exception.ToString());
            Console.WriteLine("Unhandled exception: " + exception);
        }
        catch
        {
            // Error reporting stays local and must never prevent shutdown.
        }
    }

    private void ShowSplash()
    {
        Views.SplashScreen animatedSplashScreenWindow = new();
        splashScreen = animatedSplashScreenWindow;

        animatedSplashScreenWindow.Show();

        ResetSplashCreated.Set();
        Dispatcher.Run();
    }

    public static void ChangeTheme(bool isDarkMode)
    {
        Uri uri = isDarkMode ? new Uri("Themes/Dark.xaml", UriKind.Relative) : new Uri("Themes/Light.xaml", UriKind.Relative);
        ResourceDictionary theme = new() { Source = uri };
        ResourceDictionary shared = new() { Source = new Uri("Themes/Shared.xaml", UriKind.Relative) };

        Current.Resources.MergedDictionaries.Clear();
        Current.Resources.MergedDictionaries.Add(theme);
        Current.Resources.MergedDictionaries.Add(shared);

        grzyClothTool.MainWindow.Instance?.UpdateAvalonDockTheme(isDarkMode);
    }
}
