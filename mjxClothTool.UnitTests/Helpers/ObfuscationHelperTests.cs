using mjxClothTool.Helpers;

namespace mjxClothTool.UnitTests.Helpers;

public class ObfuscationHelperTests
{
    [Fact]
    public void HashString_ReturnsUppercaseSha256Hash()
    {
        var hash = ObfuscationHelper.HashString("mjxClothTool");

        Assert.Equal("ED74F0A0E2E0F041EAF05C4BFB660AC8A31E8324CFE36A3247624567B6507B6F", hash);
    }

    [Fact]
    public async Task XORFile_ObfuscatesAndCanRoundTripFileContent()
    {
        using var temp = new TestTempDirectory();
        var sourcePath = temp.FilePath("source.bin");
        var obfuscatedPath = temp.FilePath("obfuscated.bin");
        var restoredPath = temp.FilePath("restored.bin");
        var originalBytes = Enumerable.Range(0, 512).Select(i => (byte)(i % 256)).ToArray();
        await File.WriteAllBytesAsync(sourcePath, originalBytes);

        await ObfuscationHelper.XORFile(sourcePath, obfuscatedPath);
        await ObfuscationHelper.XORFile(obfuscatedPath, restoredPath);

        Assert.NotEqual(originalBytes, await File.ReadAllBytesAsync(obfuscatedPath));
        Assert.Equal(originalBytes, await File.ReadAllBytesAsync(restoredPath));
    }
}
