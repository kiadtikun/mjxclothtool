using System.Collections.Generic;
using mjxClothTool.Models.Drawable;
using static mjxClothTool.Enums;

namespace mjxClothTool.Helpers;

public class SimplePathBuilder
{
    public static string BuildPath(GDrawable drawable, string buildPath, BuildResourceType? resourceType = null)
    {
        var pathParts = new List<string> { buildPath, "stream" };
        var groupAlreadyEndsWithType = false;

        var genderFolder = drawable.Sex == SexType.male ? "[male]" : "[female]";
        pathParts.Add(genderFolder);

        if (resourceType == BuildResourceType.FiveM && !string.IsNullOrWhiteSpace(drawable.Group))
        {
            var groupParts = drawable.Group.Split(
                ['/', '\\'],
                System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);

            pathParts.AddRange(groupParts);
            groupAlreadyEndsWithType = groupParts.Length > 0 &&
                string.Equals(groupParts[^1], drawable.TypeName, System.StringComparison.OrdinalIgnoreCase);
        }

        if (!groupAlreadyEndsWithType)
        {
            pathParts.Add(drawable.TypeName);
        }

        return System.IO.Path.Combine([.. pathParts]);
    }
}
