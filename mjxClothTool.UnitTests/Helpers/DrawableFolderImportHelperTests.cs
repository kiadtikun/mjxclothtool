using mjxClothTool.Helpers;

namespace mjxClothTool.UnitTests.Helpers;

public class DrawableFolderImportHelperTests
{
    [Theory]
    [InlineData("talecloth_1", "talecloth_2", "talecloth_10")]
    [InlineData("talecloth_01", "talecloth_02", "talecloth_05")]
    public void OrdersFoldersBeforeDrawableNumbers(string first, string second, string last)
    {
        using var temp = new TestTempDirectory();
        string Add(string folder, string name)
        {
            var path = temp.FilePath(folder, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "test");
            return path;
        }

        var firstHigh = Add(first, "jbib_010_u.ydd");
        var firstLow = Add(first, "jbib_002_u.ydd");
        var secondFile = Add(second, "jbib_000_u.ydd");
        var lastFile = Add(last, "jbib_000_u.ydd");

        var batches = DrawableFolderImportHelper.GetOrderedBatches(
            new[] { Path.GetDirectoryName(lastFile)!, Path.GetDirectoryName(secondFile)!, Path.GetDirectoryName(firstHigh)! });

        Assert.Equal(3, batches.Count);
        Assert.Equal(new[] { firstLow, firstHigh }, batches[0]);
        Assert.Equal(new[] { secondFile }, batches[1]);
        Assert.Equal(new[] { lastFile }, batches[2]);
    }

    [Fact]
    public void IncludesNestedFilesOnlyOnceWhenSelectionsOverlap()
    {
        using var temp = new TestTempDirectory();
        var path = temp.FilePath("talecloth_01", "nested", "jbib_001_u.ydd");
        var nested = Path.GetDirectoryName(path)!;
        var parent = Path.GetDirectoryName(nested)!;
        Directory.CreateDirectory(nested);
        File.WriteAllText(path, "test");
        File.WriteAllText(Path.Combine(nested, "jbib_diff_001_a_uni.ytd"), "texture");

        var batches = DrawableFolderImportHelper.GetOrderedBatches(new[] { parent, nested, parent });

        Assert.Equal(new[] { path }, batches.SelectMany(batch => batch).ToArray());
    }
}
