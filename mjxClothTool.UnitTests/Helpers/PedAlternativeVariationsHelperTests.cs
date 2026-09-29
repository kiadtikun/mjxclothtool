using mjxClothTool.Constants;
using mjxClothTool.Helpers;
using static mjxClothTool.Enums;

namespace mjxClothTool.UnitTests.Helpers;

public class PedAlternativeVariationsHelperTests
{
    [Theory]
    [InlineData(SexType.male)]
    [InlineData(SexType.female)]
    public void GetHairEntriesForSex_ReturnsConfiguredListForSex(SexType sex)
    {
        var entries = PedAlternativeVariationsHelper.GetHairEntriesForSex(sex);

        Assert.Same(
            sex == SexType.male
                ? PedAlternateVariationsConstants.MaleHairs
                : PedAlternateVariationsConstants.FemaleHairs,
            entries);
        Assert.NotEmpty(entries);
    }

    [Theory]
    [InlineData(SexType.male)]
    [InlineData(SexType.female)]
    public void GetMaskEntriesForSex_ReturnsConfiguredListForSex(SexType sex)
    {
        var entries = PedAlternativeVariationsHelper.GetMaskEntriesForSex(sex);

        Assert.Same(
            sex == SexType.male
                ? PedAlternateVariationsConstants.MaleMasks
                : PedAlternateVariationsConstants.FemaleMasks,
            entries);
        Assert.NotEmpty(entries);
    }
}
