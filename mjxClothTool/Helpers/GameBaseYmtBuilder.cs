using CodeWalker.GameFiles;
using mjxClothTool.Controls;
using mjxClothTool.Models.Drawable;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using static mjxClothTool.Enums;

namespace mjxClothTool.Helpers;

public sealed class GameBaseYmtDefinition
{
    private const string ResourcePrefix = "mjxClothTool.Resources.GameBase";

    internal byte[] AvailableComponents { get; init; } = [];
    internal List<GameBaseComponent> Components { get; init; } = [];
    internal List<GameBaseComponentInfo> ComponentInfos { get; init; } = [];
    internal List<GameBaseProp> Props { get; init; } = [];
    internal List<GameBaseAnchor> Anchors { get; init; } = [];
    internal uint DlcNameHash { get; init; }

    public IReadOnlyList<int> ComponentDrawableCounts => Components
        .Select(component => component.Drawables.Count)
        .ToArray();

    public IReadOnlyDictionary<int, int> PropDrawableCounts => Anchors
        .ToDictionary(anchor => (int)anchor.Anchor, anchor => anchor.TextureCounts.Count);

    public static GameBaseYmtDefinition Load(SexType sex)
    {
        var pedName = sex == SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
        var resourceName = $"{ResourcePrefix}.{pedName}.ymt.xml";
        var assembly = typeof(GameBaseYmtDefinition).Assembly;

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded Game Base definition '{resourceName}' was not found.");

        return Load(stream);
    }

    public static GameBaseYmtDefinition Load(Stream stream)
    {
        var root = XDocument.Load(stream).Root
            ?? throw new InvalidDataException("The Game Base YMT XML has no root element.");

        return new GameBaseYmtDefinition
        {
            AvailableComponents = ParseByteList(root.Element("availComp")?.Value),
            Components = root.Element("aComponentData3")?.Elements("Item").Select(ParseComponent).ToList() ?? [],
            ComponentInfos = root.Element("compInfos")?.Elements("Item").Select(ParseComponentInfo).ToList() ?? [],
            Props = root.Element("propInfo")?.Element("aPropMetaData")?.Elements("Item").Select(ParseProp).ToList() ?? [],
            Anchors = root.Element("propInfo")?.Element("aAnchors")?.Elements("Item").Select(ParseAnchor).ToList() ?? [],
            DlcNameHash = ParseHash(root.Element("dlcName")?.Value)
        };
    }

    private static GameBaseComponent ParseComponent(XElement element)
    {
        return new GameBaseComponent
        {
            NumAvailableTextures = ParseByteValue(element.Element("numAvailTex")),
            Drawables = element.Element("aDrawblData3")?.Elements("Item").Select(ParseDrawable).ToList() ?? []
        };
    }

    private static GameBaseDrawable ParseDrawable(XElement element)
    {
        return new GameBaseDrawable
        {
            PropMask = ParseByteValue(element.Element("propMask")),
            NumAlternatives = ParseByteValue(element.Element("numAlternatives")),
            OwnsCloth = ParseBooleanValue(element.Element("clothData")?.Element("ownsCloth")),
            Textures = element.Element("aTexData")?.Elements("Item").Select(texture => new GameBaseTexture
            {
                TextureId = ParseByteValue(texture.Element("texId")),
                Distribution = ParseByteValue(texture.Element("distribution"))
            }).ToList() ?? []
        };
    }

    private static GameBaseComponentInfo ParseComponentInfo(XElement element)
    {
        var expression = ParseFloatList(element.Element("hash_07AE529D")?.Value);
        return new GameBaseComponentInfo
        {
            AudioId = ParseHash(element.Element("hash_2FD08CEF")?.Value),
            AudioId2 = ParseHash(element.Element("hash_FC507D28")?.Value),
            ExpressionMods = expression,
            Flags = ParseUIntValue(element.Element("flags")),
            Inclusions = ParseUIntText(element.Element("inclusions")?.Value),
            Exclusions = ParseUIntText(element.Element("exclusions")?.Value),
            VfxComponent = ParseEnum(element.Element("hash_6032815C")?.Value, ePedVarComp.PV_COMP_HEAD),
            PedFlags = ParseByteValue(element.Element("hash_7E103C8B")),
            ComponentIndex = ParseByteValue(element.Element("hash_D12F579D")),
            DrawableIndex = ParseByteValue(element.Element("hash_FA1F27BF"))
        };
    }

    private static GameBaseProp ParseProp(XElement element)
    {
        var expression = ParseFloatList(element.Element("expressionMods")?.Value);
        return new GameBaseProp
        {
            AudioId = ParseHash(element.Element("audioId")?.Value),
            ExpressionMods = expression,
            Textures = element.Element("texData")?.Elements("Item").Select(texture => new GameBasePropTexture
            {
                Inclusions = ParseUIntText(texture.Element("inclusions")?.Value),
                Exclusions = ParseUIntText(texture.Element("exclusions")?.Value),
                TextureId = ParseByteValue(texture.Element("texId")),
                InclusionId = ParseByteValue(texture.Element("inclusionId")),
                ExclusionId = ParseByteValue(texture.Element("exclusionId")),
                Distribution = ParseByteValue(texture.Element("distribution"))
            }).ToList() ?? [],
            RenderFlags = ParseFlags<ePropRenderFlags>(element.Element("renderFlags")?.Value),
            PropFlags = ParseUIntValue(element.Element("propFlags")),
            Flags = ParseUIntValue(element.Element("flags")),
            AnchorId = ParseByteValue(element.Element("anchorId")),
            PropId = ParseByteValue(element.Element("propId")),
            Unknown = ParseUIntValue(element.Element("hash_AC887A91"))
        };
    }

    private static GameBaseAnchor ParseAnchor(XElement element)
    {
        return new GameBaseAnchor
        {
            Anchor = ParseEnum(element.Element("anchor")?.Value, eAnchorPoints.ANCHOR_HEAD),
            TextureCounts = ParseByteList(element.Element("props")?.Value).ToList()
        };
    }

    private static byte ParseByteValue(XElement element)
    {
        var value = element?.Attribute("value")?.Value ?? element?.Value;
        return byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : (byte)0;
    }

    private static uint ParseUIntValue(XElement element)
    {
        var value = element?.Attribute("value")?.Value ?? element?.Value;
        return ParseUIntText(value);
    }

    private static uint ParseUIntText(string value)
    {
        return uint.TryParse(value?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static bool ParseBooleanValue(XElement element)
    {
        var value = element?.Attribute("value")?.Value ?? element?.Value;
        return bool.TryParse(value, out var result) && result;
    }

    private static uint ParseHash(string value)
    {
        value = value?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        return uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
            ? numeric
            : JenkHash.GenHash(value);
    }

    private static byte[] ParseByteList(string value)
    {
        return SplitValues(value)
            .Select(item => byte.Parse(item, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static float[] ParseFloatList(string value)
    {
        var values = SplitValues(value)
            .Select(item => float.Parse(item, CultureInfo.InvariantCulture))
            .ToArray();

        Array.Resize(ref values, 5);
        return values;
    }

    private static IEnumerable<string> SplitValues(string value)
    {
        return (value ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct, Enum
    {
        value = value?.Trim();
        if (Enum.TryParse<TEnum>(value, true, out var parsed))
        {
            return parsed;
        }

        if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), numeric);
        }

        return fallback;
    }

    private static TEnum ParseFlags<TEnum>(string value) where TEnum : struct, Enum
    {
        ulong result = 0;
        foreach (var token in (value ?? string.Empty).Split([' ', ',', '|'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<TEnum>(token, true, out var parsed))
            {
                result |= Convert.ToUInt64(parsed, CultureInfo.InvariantCulture);
            }
        }

        return (TEnum)Enum.ToObject(typeof(TEnum), result);
    }
}

public static class GameBaseYmtBuilder
{
    public static Dictionary<GDrawable, int> GetBuildNumberMap(
        GameBaseYmtDefinition definition,
        IEnumerable<GDrawable> drawables)
    {
        var result = new Dictionary<GDrawable, int>();
        var comparer = new DrawableGroupComparer();

        foreach (var group in drawables.GroupBy(drawable => (drawable.IsProp, drawable.TypeNumeric)))
        {
            var baseCount = group.Key.IsProp
                ? definition.PropDrawableCounts.GetValueOrDefault(group.Key.TypeNumeric)
                : group.Key.TypeNumeric >= 0 && group.Key.TypeNumeric < definition.ComponentDrawableCounts.Count
                    ? definition.ComponentDrawableCounts[group.Key.TypeNumeric]
                    : 0;

            var sorted = group.OrderBy(drawable => drawable, comparer).ToList();
            for (var index = 0; index < sorted.Count; index++)
            {
                result[sorted[index]] = baseCount + index;
            }
        }

        return result;
    }

    public static byte[] Build(
        GameBaseYmtDefinition definition,
        IEnumerable<GDrawable> sourceDrawables,
        string pedName)
    {
        var customDrawables = sourceDrawables.ToList();
        var buildNumberMap = GetBuildNumberMap(definition, customDrawables);
        ValidateLimits(buildNumberMap);

        var mb = new MetaBuilder();
        mb.EnsureBlock(MetaName.CPedVariationInfo);

        var ped = new CPedVariationInfo
        {
            bHasDrawblVariations = 1,
            bHasTexVariations = 1,
            bHasLowLODs = 0,
            bIsSuperLOD = 0,
            dlcName = definition.DlcNameHash
        };

        var availableComponents = new ArrayOfBytes12();
        availableComponents.SetBytes(definition.AvailableComponents);
        ped.availComp = availableComponents;

        var componentCount = Math.Max(12, definition.Components.Count);
        var components = new CPVComponentData[componentCount];
        for (var componentIndex = 0; componentIndex < componentCount; componentIndex++)
        {
            var baseComponent = componentIndex < definition.Components.Count
                ? definition.Components[componentIndex]
                : new GameBaseComponent();
            var customForComponent = customDrawables
                .Where(drawable => !drawable.IsProp && drawable.TypeNumeric == componentIndex)
                .OrderBy(drawable => buildNumberMap[drawable])
                .ToList();

            var drawableData = baseComponent.Drawables
                .Select(drawable => CreateBaseDrawableData(mb, drawable))
                .Concat(customForComponent.Select(drawable => CreateCustomDrawableData(mb, drawable)))
                .ToArray();
            var textureCount = baseComponent.NumAvailableTextures + customForComponent.Sum(drawable => drawable.Textures.Count);

            components[componentIndex] = new CPVComponentData
            {
                // The game stores this total in one byte. Original freemode data also wraps
                // 256 textures to 0, so keep the same byte-overflow behavior when appending.
                numAvailTex = unchecked((byte)textureCount),
                aDrawblData3 = mb.AddItemArrayPtr(MetaName.CPVDrawblData, drawableData)
            };
        }
        ped.aComponentData3 = mb.AddItemArrayPtr(MetaName.CPVComponentData, components);

        var componentInfos = definition.ComponentInfos.Select(CreateBaseComponentInfo).ToList();
        componentInfos.AddRange(customDrawables
            .Where(drawable => !drawable.IsProp)
            .OrderBy(drawable => drawable.TypeNumeric)
            .ThenBy(drawable => buildNumberMap[drawable])
            .Select(drawable => CreateCustomComponentInfo(drawable, buildNumberMap[drawable])));
        ped.compInfos = mb.AddItemArrayPtr(MetaName.CComponentInfo, componentInfos.ToArray());

        var propMetadata = definition.Props.Select(prop => CreateBasePropData(mb, prop)).ToList();
        propMetadata.AddRange(customDrawables
            .Where(drawable => drawable.IsProp)
            .OrderBy(drawable => drawable.TypeNumeric)
            .ThenBy(drawable => buildNumberMap[drawable])
            .Select(drawable => CreateCustomPropData(mb, drawable, buildNumberMap[drawable])));

        var propInfo = new CPedPropInfo
        {
            numAvailProps = unchecked((byte)propMetadata.Count),
            aPropMetaData = mb.AddItemArrayPtr(MetaName.CPedPropMetaData, propMetadata.ToArray())
        };

        var anchorsById = definition.Anchors.ToDictionary(anchor => (int)anchor.Anchor);
        foreach (var customAnchor in customDrawables.Where(drawable => drawable.IsProp).GroupBy(drawable => drawable.TypeNumeric))
        {
            if (!anchorsById.ContainsKey(customAnchor.Key))
            {
                anchorsById[customAnchor.Key] = new GameBaseAnchor
                {
                    Anchor = (eAnchorPoints)customAnchor.Key,
                    TextureCounts = []
                };
            }
        }

        var anchors = anchorsById.OrderBy(pair => pair.Key).Select(pair =>
        {
            var textureCounts = pair.Value.TextureCounts.ToList();
            textureCounts.AddRange(customDrawables
                .Where(drawable => drawable.IsProp && drawable.TypeNumeric == pair.Key)
                .OrderBy(drawable => buildNumberMap[drawable])
                .Select(drawable => checked((byte)drawable.Textures.Count)));

            return new CAnchorProps
            {
                anchor = pair.Value.Anchor,
                props = mb.AddByteArrayPtr(textureCounts.ToArray())
            };
        }).ToArray();
        propInfo.aAnchors = mb.AddItemArrayPtr(MetaName.CAnchorProps, anchors);
        ped.propInfo = propInfo;

        mb.AddItem(MetaName.CPedVariationInfo, ped);
        AddStructureDefinitions(mb);

        var meta = mb.GetMeta();
        meta.Name = pedName;
        return ResourceBuilder.Build(meta, 2);
    }

    private static CPVDrawblData CreateBaseDrawableData(MetaBuilder mb, GameBaseDrawable drawable)
    {
        return new CPVDrawblData
        {
            propMask = drawable.PropMask,
            numAlternatives = drawable.NumAlternatives,
            aTexData = mb.AddItemArrayPtr(MetaName.CPVTextureData, drawable.Textures.Select(texture => new CPVTextureData
            {
                texId = texture.TextureId,
                distribution = texture.Distribution
            }).ToArray()),
            clothData = new CPVDrawblData__CPVClothComponentData { ownsCloth = drawable.OwnsCloth ? (byte)1 : (byte)0 }
        };
    }

    private static CPVDrawblData CreateCustomDrawableData(MetaBuilder mb, GDrawable drawable)
    {
        return new CPVDrawblData
        {
            propMask = (byte)(drawable.HasSkin ? 17 : 1),
            numAlternatives = (byte)(string.IsNullOrEmpty(drawable.FirstPersonPath) ? 0 : 1),
            aTexData = mb.AddItemArrayPtr(MetaName.CPVTextureData, drawable.Textures.Select(_ => new CPVTextureData
            {
                texId = (byte)(drawable.HasSkin ? 1 : 0),
                distribution = 255
            }).ToArray()),
            clothData = new CPVDrawblData__CPVClothComponentData
            {
                ownsCloth = (byte)(string.IsNullOrEmpty(drawable.ClothPhysicsPath) ? 0 : 1)
            }
        };
    }

    private static CComponentInfo CreateBaseComponentInfo(GameBaseComponentInfo info)
    {
        return new CComponentInfo
        {
            pedXml_audioID = info.AudioId,
            pedXml_audioID2 = info.AudioId2,
            pedXml_expressionMods = ToFloatArray(info.ExpressionMods),
            flags = info.Flags,
            inclusions = checked((int)info.Inclusions),
            exclusions = checked((int)info.Exclusions),
            pedXml_vfxComps = info.VfxComponent,
            pedXml_flags = info.PedFlags,
            pedXml_compIdx = info.ComponentIndex,
            pedXml_drawblIdx = info.DrawableIndex
        };
    }

    private static CComponentInfo CreateCustomComponentInfo(GDrawable drawable, int buildNumber)
    {
        return new CComponentInfo
        {
            pedXml_audioID = JenkHash.GenHash(drawable.Audio),
            pedXml_audioID2 = JenkHash.GenHash("none"),
            pedXml_expressionMods = new ArrayOfFloats5 { f4 = drawable.EnableHighHeels ? drawable.HighHeelsValue : 0 },
            flags = (uint)drawable.Flags,
            inclusions = 0,
            exclusions = 0,
            pedXml_vfxComps = ePedVarComp.PV_COMP_HEAD,
            pedXml_flags = 0,
            pedXml_compIdx = checked((byte)drawable.TypeNumeric),
            pedXml_drawblIdx = checked((byte)buildNumber)
        };
    }

    private static CPedPropMetaData CreateBasePropData(MetaBuilder mb, GameBaseProp prop)
    {
        return new CPedPropMetaData
        {
            audioId = prop.AudioId,
            expressionMods = ToFloatArray(prop.ExpressionMods),
            texData = mb.AddItemArrayPtr(MetaName.CPedPropTexData, prop.Textures.Select(texture => new CPedPropTexData
            {
                inclusions = checked((int)texture.Inclusions),
                exclusions = checked((int)texture.Exclusions),
                texId = texture.TextureId,
                inclusionId = texture.InclusionId,
                exclusionId = texture.ExclusionId,
                distribution = texture.Distribution
            }).ToArray()),
            renderFlags = prop.RenderFlags,
            propFlags = prop.PropFlags,
            flags = checked((ushort)prop.Flags),
            anchorId = prop.AnchorId,
            propId = prop.PropId,
            Unk_2894625425 = checked((byte)prop.Unknown)
        };
    }

    private static CPedPropMetaData CreateCustomPropData(MetaBuilder mb, GDrawable drawable, int buildNumber)
    {
        ePropRenderFlags renderFlags = 0;
        Enum.TryParse(drawable.RenderFlag, out renderFlags);

        return new CPedPropMetaData
        {
            audioId = JenkHash.GenHash(drawable.Audio),
            expressionMods = new ArrayOfFloats5 { f0 = drawable.EnableHairScale ? -drawable.HairScaleValue : 0 },
            texData = mb.AddItemArrayPtr(MetaName.CPedPropTexData, drawable.Textures.Select((_, index) => new CPedPropTexData
            {
                texId = checked((byte)index),
                distribution = 255
            }).ToArray()),
            renderFlags = renderFlags,
            propFlags = (uint)drawable.Flags,
            flags = 0,
            anchorId = checked((byte)drawable.TypeNumeric),
            propId = checked((byte)buildNumber),
            Unk_2894625425 = 0
        };
    }

    private static ArrayOfFloats5 ToFloatArray(IReadOnlyList<float> values)
    {
        return new ArrayOfFloats5
        {
            f0 = values.Count > 0 ? values[0] : 0,
            f1 = values.Count > 1 ? values[1] : 0,
            f2 = values.Count > 2 ? values[2] : 0,
            f3 = values.Count > 3 ? values[3] : 0,
            f4 = values.Count > 4 ? values[4] : 0
        };
    }

    private static void ValidateLimits(IReadOnlyDictionary<GDrawable, int> buildNumberMap)
    {
        var invalid = buildNumberMap.FirstOrDefault(pair => pair.Value > byte.MaxValue);
        if (invalid.Key != null)
        {
            throw new InvalidOperationException(
                $"'{invalid.Key.DisplayName}' would use Game Base drawable number {invalid.Value}, but the YMT limit is 255.");
        }
    }

    private static void AddStructureDefinitions(MetaBuilder mb)
    {
        mb.AddStructureInfo(MetaName.CPedVariationInfo);
        mb.AddStructureInfo(MetaName.CPedPropInfo);
        mb.AddStructureInfo(MetaName.CPedPropTexData);
        mb.AddStructureInfo(MetaName.CAnchorProps);
        mb.AddStructureInfo(MetaName.CComponentInfo);
        mb.AddStructureInfo(MetaName.CPVComponentData);
        mb.AddStructureInfo(MetaName.CPVDrawblData);
        mb.AddStructureInfo(MetaName.CPVDrawblData__CPVClothComponentData);
        mb.AddStructureInfo(MetaName.CPVTextureData);
        mb.AddStructureInfo(MetaName.CPedPropMetaData);
        mb.AddEnumInfo(MetaName.ePedVarComp);
        mb.AddEnumInfo(MetaName.eAnchorPoints);
        mb.AddEnumInfo(MetaName.ePropRenderFlags);
    }
}

internal sealed class GameBaseComponent
{
    public byte NumAvailableTextures { get; init; }
    public List<GameBaseDrawable> Drawables { get; init; } = [];
}

internal sealed class GameBaseDrawable
{
    public byte PropMask { get; init; }
    public byte NumAlternatives { get; init; }
    public bool OwnsCloth { get; init; }
    public List<GameBaseTexture> Textures { get; init; } = [];
}

internal sealed class GameBaseTexture
{
    public byte TextureId { get; init; }
    public byte Distribution { get; init; }
}

internal sealed class GameBaseComponentInfo
{
    public uint AudioId { get; init; }
    public uint AudioId2 { get; init; }
    public float[] ExpressionMods { get; init; } = new float[5];
    public uint Flags { get; init; }
    public uint Inclusions { get; init; }
    public uint Exclusions { get; init; }
    public ePedVarComp VfxComponent { get; init; }
    public byte PedFlags { get; init; }
    public byte ComponentIndex { get; init; }
    public byte DrawableIndex { get; init; }
}

internal sealed class GameBaseProp
{
    public uint AudioId { get; init; }
    public float[] ExpressionMods { get; init; } = new float[5];
    public List<GameBasePropTexture> Textures { get; init; } = [];
    public ePropRenderFlags RenderFlags { get; init; }
    public uint PropFlags { get; init; }
    public uint Flags { get; init; }
    public byte AnchorId { get; init; }
    public byte PropId { get; init; }
    public uint Unknown { get; init; }
}

internal sealed class GameBasePropTexture
{
    public uint Inclusions { get; init; }
    public uint Exclusions { get; init; }
    public byte TextureId { get; init; }
    public byte InclusionId { get; init; }
    public byte ExclusionId { get; init; }
    public byte Distribution { get; init; }
}

internal sealed class GameBaseAnchor
{
    public eAnchorPoints Anchor { get; init; }
    public List<byte> TextureCounts { get; init; } = [];
}
