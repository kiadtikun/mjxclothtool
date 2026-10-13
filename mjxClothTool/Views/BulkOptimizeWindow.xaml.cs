using mjxClothTool.Helpers;
using mjxClothTool.Models.Drawable;
using mjxClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace mjxClothTool.Views;

public partial class BulkOptimizeWindow : Window
{
    private const int MaxTextureDimension = 2048;
    private bool _isApplying;

    public BulkOptimizeWindow()
    {
        InitializeComponent();
        Closing += (_, e) => e.Cancel = _isApplying;
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        var downscale = DownscaleCheckBox.IsChecked == true;
        var changeCompression = CompressionCheckBox.IsChecked == true;
        var mipmapChoice = (MipmapComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Keep original";
        if (!downscale && !changeCompression && mipmapChoice == "Keep original")
        {
            StatusText.Text = "Select at least one optimization option.";
            return;
        }

        var drawables = MainWindow.AddonManager.Addons.SelectMany(addon => addon.Drawables).ToList();
        var textures = drawables.SelectMany(drawable => drawable.Textures).Distinct().ToList();
        var embedded = drawables
            .SelectMany(drawable => drawable.Details?.EmbeddedTextures?.Values ??
                Enumerable.Empty<GTextureEmbedded>())
            .Where(texture => texture != null)
            .Distinct()
            .ToList();
        var total = textures.Count + embedded.Count;
        if (total == 0)
        {
            StatusText.Text = "No textures found in this project.";
            return;
        }

        _isApplying = true;
        ApplyButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        DownscaleCheckBox.IsEnabled = false;
        CompressionCheckBox.IsEnabled = false;
        MipmapComboBox.IsEnabled = false;
        OptimizeProgress.Maximum = total;
        OptimizeProgress.Value = 0;

        var applied = 0;
        var skipped = 0;
        var processed = 0;
        try
        {
            foreach (var texture in textures)
            {
                StatusText.Text = $"Applying settings: {++processed}/{total} — {texture.DisplayName}";
                if (TryCreateDetails(texture.TxtDetails, downscale, changeCompression, mipmapChoice, out var details))
                {
                    texture.OptimizeDetails = details;
                    texture.IsOptimizedDuringBuild = true;
                    applied++;
                }
                else skipped++;

                OptimizeProgress.Value = processed;
                await Task.Yield();
            }

            foreach (var texture in embedded)
            {
                StatusText.Text = $"Applying settings: {++processed}/{total} — {texture.Details?.Name}";
                if (TryCreateDetails(texture.Details, downscale, changeCompression, mipmapChoice, out var details))
                {
                    texture.OptimizeDetails = details;
                    texture.IsOptimizedDuringBuild = true;
                    applied++;
                }
                else skipped++;

                OptimizeProgress.Value = processed;
                await Task.Yield();
            }

            if (applied > 0) SaveHelper.SetUnsavedChanges(true);
            StatusText.Text = $"Ready for Build: {applied} texture(s); skipped: {skipped}.";
            LogHelper.Log($"Optimization settings applied to {applied} textures; {skipped} skipped.");
        }
        catch (Exception ex)
        {
            if (applied > 0) SaveHelper.SetUnsavedChanges(true);
            StatusText.Text = $"Stopped after {processed}/{total}: {ex.Message}";
            LogHelper.Log($"Bulk texture optimization failed: {ex}", LogType.Error);
        }
        finally
        {
            _isApplying = false;
            CloseButton.IsEnabled = true;
            ApplyButton.IsEnabled = true;
            DownscaleCheckBox.IsEnabled = true;
            CompressionCheckBox.IsEnabled = true;
            MipmapComboBox.IsEnabled = true;
        }
    }

    private static bool TryCreateDetails(GTextureDetails source, bool downscale, bool changeCompression,
        string mipmapChoice,
        out GTextureDetails result)
    {
        result = null;
        if (source == null || source.Width <= 0 || source.Height <= 0) return false;

        var needsDownscale = downscale &&
            (source.Width > MaxTextureDimension || source.Height > MaxTextureDimension);
        if (!needsDownscale && !changeCompression && mipmapChoice == "Keep original") return false;

        var (width, height) = ImgHelper.CheckPowerOfTwo(source.Width, source.Height);
        if (needsDownscale)
        {
            var newWidth = width;
            var newHeight = height;
            while (newWidth > MaxTextureDimension || newHeight > MaxTextureDimension)
            {
                newWidth /= 2;
                newHeight /= 2;
            }
            if (newWidth >= 4 && newHeight >= 4)
            {
                width = newWidth;
                height = newHeight;
            }
            else if (!changeCompression && mipmapChoice == "Keep original") return false;
        }

        var maximumMipmaps = Math.Max(1, ImgHelper.GetCorrectMipMapAmount(width, height));
        var mipmapCount = mipmapChoice switch
        {
            "Keep original" when width == source.Width && height == source.Height => source.MipMapCount,
            "Keep original" => Math.Min(Math.Max(1, source.MipMapCount), maximumMipmaps),
            "Auto" => maximumMipmaps,
            _ => Math.Min(int.Parse(mipmapChoice), maximumMipmaps)
        };

        result = new GTextureDetails
        {
            Name = source.Name,
            Width = width,
            Height = height,
            Compression = changeCompression ? "D3DFMT_DXT5" : source.Compression,
            MipMapCount = mipmapCount,
            IsOptimizeNeeded = false
        };
        return true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
