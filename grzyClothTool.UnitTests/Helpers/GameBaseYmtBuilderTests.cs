using System.Collections.ObjectModel;
using CodeWalker.GameFiles;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using static grzyClothTool.Enums;

namespace grzyClothTool.UnitTests.Helpers;

public class GameBaseYmtBuilderTests
{
    [Theory]
    [InlineData(SexType.male, 39)]
    [InlineData(SexType.female, 38)]
    public void EmbeddedDefinition_LoadsComponentsAndProps(SexType sex, int expectedPropCount)
    {
        var definition = GameBaseYmtDefinition.Load(sex);

        Assert.Equal(12, definition.ComponentDrawableCounts.Count);
        Assert.Equal(16, definition.ComponentDrawableCounts[(int)ComponentNumbers.jbib]);
        Assert.Equal(expectedPropCount, definition.PropDrawableCounts.Values.Sum());
    }

    [Fact]
    public void BuildNumberMap_AppendsAfterEachGameBaseCollection()
    {
        var definition = GameBaseYmtDefinition.Load(SexType.male);
        var firstJbib = CreateDrawable(isProp: false, (int)ComponentNumbers.jbib, number: 0);
        var secondJbib = CreateDrawable(isProp: false, (int)ComponentNumbers.jbib, number: 1);
        var hat = CreateDrawable(isProp: true, (int)PropNumbers.p_head, number: 0);

        var map = GameBaseYmtBuilder.GetBuildNumberMap(definition, [secondJbib, hat, firstJbib]);

        Assert.Equal(definition.ComponentDrawableCounts[(int)ComponentNumbers.jbib], map[firstJbib]);
        Assert.Equal(map[firstJbib] + 1, map[secondJbib]);
        Assert.Equal(definition.PropDrawableCounts[(int)PropNumbers.p_head], map[hat]);
    }

    [Fact]
    public void Build_ProducesMergedYmtResource()
    {
        var definition = GameBaseYmtDefinition.Load(SexType.female);
        var drawable = CreateDrawable(isProp: false, (int)ComponentNumbers.lowr, number: 0);

        var bytes = GameBaseYmtBuilder.Build(definition, [drawable], "mp_f_freemode_01");
        var ymt = new YmtFile();

        ymt.Load(bytes);

        Assert.NotEmpty(bytes);
        Assert.True(bytes.Length > 1024);
        Assert.NotNull(ymt.Meta);
    }

    [Fact]
    public void Build_AllowsComponentTextureTotalToWrapPastByteLimit()
    {
        var definition = GameBaseYmtDefinition.Load(SexType.male);
        var drawable = CreateDrawable(
            isProp: false,
            (int)ComponentNumbers.accs,
            number: 0,
            textureCount: 20);

        var bytes = GameBaseYmtBuilder.Build(definition, [drawable], "mp_m_freemode_01");
        var ymt = new YmtFile();

        ymt.Load(bytes);

        Assert.NotNull(ymt.Meta);
    }

    private static GDrawable CreateDrawable(bool isProp, int typeNumeric, int number, int textureCount = 1)
    {
        return new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            SexType.male,
            isProp,
            typeNumeric,
            number,
            hasSkin: false,
            new ObservableCollection<GTexture>(Enumerable.Range(0, textureCount).Select(textureNumber =>
                new GTexture(
                    Guid.NewGuid(),
                    Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd"),
                    typeNumeric,
                    number,
                    textureNumber,
                    false,
                    isProp))));
    }
}
