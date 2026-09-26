using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace grzyClothTool.Helpers;

public static class DrawableFolderImportHelper
{
    public static List<string[]> GetOrderedBatches(IEnumerable<string> folders)
    {
        var naturalComparer = StringComparer.Create(CultureInfo.InvariantCulture,
            CompareOptions.IgnoreCase | CompareOptions.NumericOrdering);
        var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batches = new List<string[]>();

        foreach (var folder in folders.Select(Path.GetFullPath)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .OrderBy(path => Path.GetFileName(Path.TrimEndingDirectorySeparator(path)), naturalComparer)
                     .ThenBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            var files = Directory.GetFiles(folder, "*.ydd", SearchOption.AllDirectories)
                .Where(seenFiles.Add)
                .OrderBy(path => FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(path)) ?? int.MaxValue)
                .ThenBy(Path.GetFileName, naturalComparer)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (files.Length > 0)
                batches.Add(files);
        }

        return batches;
    }
}
