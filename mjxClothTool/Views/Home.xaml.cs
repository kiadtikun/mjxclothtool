using mjxClothTool.Constants;
using mjxClothTool.Helpers;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using static mjxClothTool.Controls.CustomMessageBox;

namespace mjxClothTool.Views
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        private ObservableCollection<RecentProject> _recentlyOpened;
        public ObservableCollection<RecentProject> RecentlyOpened
        {
            get => _recentlyOpened;
            set
            {
                _recentlyOpened = value;
                OnPropertyChanged(nameof(RecentlyOpened));
                OnPropertyChanged(nameof(ShowNoRecentProjects));
            }
        }

        public bool ShowNoRecentProjects => RecentlyOpened == null || RecentlyOpened.Count == 0;

        public Home()
        {
            InitializeComponent();
            DataContext = this;

            LoadRecentProjects();
        }

        private void LoadRecentProjects()
        {
            var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
            var validProjects = recentProjects.Where(p => File.Exists(p.FilePath)).ToList();
            
            if (validProjects.Count != recentProjects.Count)
            {
                PersistentSettingsHelper.Instance.RecentlyOpenedProjects = validProjects;
            }
            
            RecentlyOpened = new ObservableCollection<RecentProject>(validProjects);
        }

        private async void CreateNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (string.IsNullOrEmpty(mainProjectsFolder))
                {
                    Show("Please configure the main projects folder in settings first.", 
                         "Configuration Required", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                if (!Directory.Exists(mainProjectsFolder))
                {
                    Show($"Main projects folder does not exist: {mainProjectsFolder}\n\nPlease update it in settings.", 
                         "Folder Not Found", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                var dialog = ProjectSetupDialog.ShowForNewProject(Window.GetWindow(this));
                if (!dialog.Confirmed)
                {
                    return;
                }

                var projectName = dialog.ProjectName.Trim();
                var isExternal = !dialog.IsSelfContained;

                if (projectName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                {
                    Show("Project name contains invalid characters. Please choose a different name.", 
                         "Invalid Name", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Warning);
                    return;
                }

                var projectFolder = Path.Combine(mainProjectsFolder, projectName);

                if (Directory.Exists(projectFolder))
                {
                    ClearProjectFolder(projectFolder);
                }

                Directory.CreateDirectory(projectFolder);
                if (!isExternal)
                {
                    var assetsFolder = Path.Combine(projectFolder, GlobalConstants.ASSETS_FOLDER_NAME);
                    Directory.CreateDirectory(assetsFolder);
                }

                MainWindow.AddonManager.Addons.Clear();
                MainWindow.AddonManager.Groups.Clear();
                MainWindow.AddonManager.Tags.Clear();
                
                MainWindow.AddonManager.ProjectName = projectName;
                MainWindow.AddonManager.IsExternalProject = isExternal;
                MainWindow.AddonManager.CreateAddon();

                SaveHelper.SetUnsavedChanges(true);
                await SaveHelper.SaveAsync();

                var saveFileName = SaveHelper.GetSaveFileName(isExternal);
                var newProjectAutoSavePath = Path.Combine(projectFolder, saveFileName);
                PersistentSettingsHelper.Instance.AddRecentProject(
                    newProjectAutoSavePath,
                    projectName,
                    drawableCount: 0,
                    addonCount: 1,
                    isExternal: isExternal
                );
                
                LoadRecentProjects();

                var projectType = isExternal ? "External" : "Self-contained";
                LogHelper.Log($"Created new {projectType} project: {projectName} at {projectFolder}");
                MainWindow.NavigationHelper.Navigate("Project");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to create new project: {ex.Message}", Views.LogType.Error);
                Show($"Failed to create new project: {ex.Message}", 
                     "Error", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void OpenAddon_Click(object sender, RoutedEventArgs e)
        {
            var success = await MainWindow.Instance.OpenAddonAsync(true);
            if (success)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
        }

        private async void ImportProject_Click(object sender, RoutedEventArgs e)
        {
            var success = await MainWindow.Instance.ImportProjectAsync(true);
            if (success)
            {
                MainWindow.NavigationHelper.Navigate("Project");
            }
        }

        private async void OpenSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Title = "Open Save File",
                    Filter = "Save files (*.json)|*.json|All files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(openFileDialog.FileName);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
            }
            catch (Exception ex)
            {
                Show($"Failed to load save: {ex.Message}", 
                     "Error", 
                     CustomMessageBoxButtons.OKOnly, 
                     CustomMessageBoxIcon.Error);
            }
        }

        private async void RecentProject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        Show("This save file no longer exists.", 
                             "File Not Found", 
                             CustomMessageBoxButtons.OKOnly, 
                             CustomMessageBoxIcon.Warning);
                        
                        var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                        recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                        PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                        LoadRecentProjects();
                        return;
                    }

                    if (!SaveHelper.CheckUnsavedChangesMessage())
                    {
                        return;
                    }

                    await SaveHelper.LoadSaveFileAsync(filePath);
                    LoadRecentProjects();
                    MainWindow.NavigationHelper.Navigate("Project");
                }
                catch (Exception ex)
                {
                    Show($"Failed to load save: {ex.Message}", 
                         "Error", 
                         CustomMessageBoxButtons.OKOnly, 
                         CustomMessageBoxIcon.Error);
                }
            }
        }

        private void RemoveRecentProject_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            
            if (sender is Button button && button.Tag is string filePath)
            {
                try
                {
                    var project = PersistentSettingsHelper.Instance.RecentlyOpenedProjects
                        .FirstOrDefault(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    
                    var projectName = project?.ProjectName ?? "";
                    
                    var recentProjects = PersistentSettingsHelper.Instance.RecentlyOpenedProjects;
                    recentProjects.RemoveAll(p => p.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase));
                    PersistentSettingsHelper.Instance.RecentlyOpenedProjects = recentProjects;
                    
                    LoadRecentProjects();
                    
                    LogHelper.Log($"Removed project from recent list: {projectName}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to remove recent project: {ex.Message}", Views.LogType.Error);
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow.Close();
        }


        private static void ClearProjectFolder(string projectFolder)
        {
            try
            {
                var saveFiles = new[] 
                { 
                    Path.Combine(projectFolder, SaveHelper.AutoSaveFileName),
                    Path.Combine(projectFolder, SaveHelper.AutoSaveExternalFileName)
                };

                foreach (var saveFile in saveFiles)
                {
                    if (File.Exists(saveFile))
                    {
                        File.Delete(saveFile);
                        LogHelper.Log($"Deleted old save file: {saveFile}");
                    }
                }

                var backupFolder = Path.Combine(projectFolder, "save-backups");
                if (Directory.Exists(backupFolder))
                {
                    Directory.Delete(backupFolder, recursive: true);
                    LogHelper.Log($"Deleted old save backups folder: {backupFolder}");
                }

                var assetsFolder = Path.Combine(projectFolder, GlobalConstants.ASSETS_FOLDER_NAME);
                if (Directory.Exists(assetsFolder))
                {
                    Directory.Delete(assetsFolder, recursive: true);
                    LogHelper.Log($"Deleted old assets folder: {assetsFolder}");
                }

                LogHelper.Log($"Cleared project folder for overwrite: {projectFolder}");
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Warning: Could not fully clear project folder: {ex.Message}", LogType.Warning);
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
