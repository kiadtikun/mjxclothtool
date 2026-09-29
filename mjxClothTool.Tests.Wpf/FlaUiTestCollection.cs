namespace mjxClothTool.Tests.Wpf;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class FlaUiTestCollection : ICollectionFixture<UiTestFixture>
{
    public const string Name = "FlaUI tests";
}
