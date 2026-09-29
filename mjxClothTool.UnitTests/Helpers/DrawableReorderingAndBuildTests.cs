using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using mjxClothTool.Controls;
using mjxClothTool.Helpers;
using mjxClothTool.Models;
using mjxClothTool.Models.Drawable;
using mjxClothTool.Models.Texture;
using Xunit;
using static mjxClothTool.Enums;

namespace mjxClothTool.UnitTests.Helpers;

public class DrawableReorderingAndBuildTests
{
    private static GDrawable CreateTestDrawable(int number, int order, int typeNumeric = 11, bool isProp = false, SexType sex = SexType.male)
    {
        var texture = new GTexture(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ytd"),
            typeNumeric: typeNumeric,
            number: number,
            txtNumber: 0,
            hasSkin: false,
            isProp: isProp);

        var drawable = new GDrawable(
            Guid.NewGuid(),
            Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ydd"),
            sex,
            isProp: isProp,
            typeNumeric: typeNumeric,
            number: number,
            hasSkin: false,
            new ObservableCollection<GTexture> { texture });

        drawable.Order = order;
        return drawable;
    }

    [Fact]
    public void Order_DefaultsToNumberWhenUnassigned()
    {
        var drawable = CreateTestDrawable(number: 75, order: -1);
        Assert.Equal(75, drawable.Order);
        Assert.Equal(75, drawable.Number);
        Assert.Equal("075", drawable.DisplayNumber);
    }

    [Fact]
    public void DragReorder_PreservesOriginalNumbers_AndUpdatesOrder()
    {
        // Scenario from user: 075, 076, 102.
        // User drags 102 to before 075.
        var d075 = CreateTestDrawable(number: 75, order: 75);
        var d076 = CreateTestDrawable(number: 76, order: 76);
        var d102 = CreateTestDrawable(number: 102, order: 102);

        var list = new List<GDrawable> { d075, d076, d102 };

        // Simulate drag 102 before 075:
        int oldIndex = list.IndexOf(d102);
        list.RemoveAt(oldIndex);
        list.Insert(0, d102);

        // Assign Order = i (new drag-and-drop logic)
        for (int i = 0; i < list.Count; i++)
        {
            list[i].Order = i;
        }

        // Verify Requirement 1: UI numbers unchanged!
        Assert.Equal(102, d102.Number);
        Assert.Equal("102", d102.DisplayNumber);
        Assert.Equal("jbib_102_u", d102.Name);

        Assert.Equal(75, d075.Number);
        Assert.Equal("075", d075.DisplayNumber);
        Assert.Equal("jbib_075_u", d075.Name);

        Assert.Equal(76, d076.Number);
        Assert.Equal("076", d076.DisplayNumber);
        Assert.Equal("jbib_076_u", d076.Name);

        // Verify Order updated
        Assert.Equal(0, d102.Order);
        Assert.Equal(1, d075.Order);
        Assert.Equal(2, d076.Order);

        // Verify DrawableGroupComparer sorts them in new order: 102 -> 075 -> 076
        var sorted = list.OrderBy(d => d, new DrawableGroupComparer()).ToList();
        Assert.Same(d102, sorted[0]);
        Assert.Same(d075, sorted[1]);
        Assert.Same(d076, sorted[2]);
    }

    [Fact]
    public void BuildNumberMap_AssignsContinuousSequentialNumbers_FromBaseSlot()
    {
        // Scenario from user:
        // Order in UI is: 102 -> 075 -> 076
        // Min number is 075
        // Build must assign 075 -> 076 -> 077
        var d102 = CreateTestDrawable(number: 102, order: 0);
        var d075 = CreateTestDrawable(number: 75, order: 1);
        var d076 = CreateTestDrawable(number: 76, order: 2);

        var drawables = new List<GDrawable> { d075, d102, d076 };

        var map = BuildResourceHelper.GetBuildNumberMap(drawables);

        Assert.Equal(75, map[d102]);
        Assert.Equal(76, map[d075]);
        Assert.Equal(77, map[d076]);

        // Generated build names
        Assert.Equal("jbib_075_u", d102.GetBuildName(map[d102]));
        Assert.Equal("jbib_076_u", d075.GetBuildName(map[d075]));
        Assert.Equal("jbib_077_u", d076.GetBuildName(map[d076]));

        // Textures
        Assert.Equal("jbib_diff_075_a_uni", d102.Textures[0].GetBuildName(map[d102]));
        Assert.Equal("jbib_diff_076_a_uni", d075.Textures[0].GetBuildName(map[d075]));
        Assert.Equal("jbib_diff_077_a_uni", d076.Textures[0].GetBuildName(map[d076]));
    }

    [Fact]
    public void Serialization_PreservesOrderAndDisplayedNumber_AcrossProjectSaveAndLoad()
    {
        // Requirement 3: บันทึกแล้วเปิดโปรเจกต์ใหม่ยังคงหมายเลขที่แสดงไว้
        var manager = new AddonManager { ProjectName = "test-reordering" };
        var addon = new Addon("Addon 1");

        var d102 = CreateTestDrawable(number: 102, order: 0);
        var d075 = CreateTestDrawable(number: 75, order: 1);
        var d076 = CreateTestDrawable(number: 76, order: 2);

        addon.Drawables.Add(d102);
        addon.Drawables.Add(d075);
        addon.Drawables.Add(d076);
        manager.Addons.Add(addon);

        var json = JsonSerializer.Serialize(manager, SaveHelper.SerializerOptions);
        Assert.False(string.IsNullOrWhiteSpace(json));

        var restored = JsonSerializer.Deserialize<AddonManager>(json, SaveHelper.SerializerOptions);
        Assert.NotNull(restored);

        var restoredDrawables = restored.Addons[0].Drawables;
        Assert.Equal(3, restoredDrawables.Count);

        // Sort using UI comparer
        var sorted = restoredDrawables.OrderBy(d => d, new DrawableGroupComparer()).ToList();

        // 1st item should be 102
        Assert.Equal(102, sorted[0].Number);
        Assert.Equal("102", sorted[0].DisplayNumber);
        Assert.Equal(0, sorted[0].Order);

        // 2nd item should be 075
        Assert.Equal(75, sorted[1].Number);
        Assert.Equal("075", sorted[1].DisplayNumber);
        Assert.Equal(1, sorted[1].Order);

        // 3rd item should be 076
        Assert.Equal(76, sorted[2].Number);
        Assert.Equal("076", sorted[2].DisplayNumber);
        Assert.Equal(2, sorted[2].Order);
    }
}
