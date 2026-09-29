using CodeWalker.GameFiles;
using CodeWalker.Utils;
using mjxClothTool.Helpers;

namespace mjxClothTool.UnitTests.Helpers;

public class YtdTextureNameHelperTests
{
    [Fact]
    public async Task RenameFirstTextureAsync_UpdatesNameAndDictionaryHash()
    {
        var sourcePath = Path.Combine(AppContext.BaseDirectory, "TestData", "reservedTexture.ytd");
        var temporaryPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd");
        File.Copy(sourcePath, temporaryPath);

        try
        {
            const string expectedName = "jbib_diff_123_a_uni";
            var originalYtd = new YtdFile();
            originalYtd.Load(await File.ReadAllBytesAsync(temporaryPath));
            var originalTexture = Assert.Single(originalYtd.TextureDict.Textures.data_items);

            await YtdTextureNameHelper.RenameFirstTextureAsync(temporaryPath, expectedName);

            var ytd = new YtdFile();
            ytd.Load(await File.ReadAllBytesAsync(temporaryPath));
            var texture = Assert.Single(ytd.TextureDict.Textures.data_items);

            Assert.Equal(expectedName, texture.Name);
            Assert.Equal(JenkHash.GenHash(expectedName), texture.NameHash);
            Assert.True(ytd.TextureDict.Dict.ContainsKey(texture.NameHash));
            Assert.Equal(originalTexture.Width, texture.Width);
            Assert.Equal(originalTexture.Height, texture.Height);
            Assert.Equal(originalTexture.Format, texture.Format);
            Assert.Equal(originalTexture.Levels, texture.Levels);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }
}
