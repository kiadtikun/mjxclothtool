using CodeWalker.GameFiles;
using CodeWalker.Utils;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;

public static class YtdTextureNameHelper
{
    public static async Task<string> RenameFirstTextureAsync(string path, string newName)
    {
        if (!string.Equals(Path.GetExtension(path), ".ytd", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only .ytd files have an editable internal texture name.");
        }

        var normalizedName = (newName ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new InvalidOperationException("Texture name cannot be empty.");
        }

        if (normalizedName.Any(char.IsControl))
        {
            throw new InvalidOperationException("Texture name cannot contain control characters.");
        }

        var ytd = new YtdFile();
        ytd.Load(await File.ReadAllBytesAsync(path));

        var textures = ytd.TextureDict?.Textures?.data_items?.Where(texture => texture != null).ToList();
        if (textures == null || textures.Count == 0)
        {
            throw new InvalidOperationException("The selected .ytd does not contain a texture.");
        }

        if (textures.Skip(1).Any(texture =>
                string.Equals(texture.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A texture named '{normalizedName}' already exists in this .ytd.");
        }

        var textureToRename = textures[0];
        textureToRename.Name = normalizedName;
        textureToRename.NameHash = JenkHash.GenHash(normalizedName.ToLowerInvariant());
        ytd.TextureDict.BuildFromTextureList(textures);

        var output = ytd.Save();
        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(tempPath, output);
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        return normalizedName;
    }
}
