using RimWorld;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace nuff.ChooseYourTextures;

public abstract class Window_SelectTextureBase : Window
{
    private static readonly string[] directionSuffix =
    {
        "_north",
        "_south",
        "_east",
        "_west"
    };

    private static readonly string[] stackCountSuffix =
    {
        "_a",
        "_b",
        "_c",
        "_d"
    };

    private string searchText = "";
    private Vector2 scrollPos;

    private string _cachedSearch;
    private List<KeyValuePair<Def, (HashSet<string>, string)>> _cachedEntries;
    private float[] _prefixY;
    private float _cachedViewHeight;

    private const float TitleHeight = 40f;
    private const float SearchHeight = 32f;
    private const float RowGap = 14f;
    private const float DefLabelHeight = 26f;
    private const float ModLabelHeight = 22f;
    private const float TilePadding = 8f;
    private const float TileMinWidth = 160f;
    private const float TileMaxHeight = 160f;

    public override Vector2 InitialSize => new Vector2(1000, 750);

    public Window_SelectTextureBase()
    {
        doCloseX = true;
        closeOnAccept = false;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override void PreOpen()
    {
        base.PreOpen();
    }

    public override void PreClose()
    {
        base.PreClose();

        GraphicDatabase.Clear();
    }

    public override void DoWindowContents(Rect inRect)
    {
        // Title
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, TitleHeight), "Choose textures");
        Text.Font = GameFont.Small;

        float y = inRect.y + TitleHeight + 6f;

        // Search
        Rect searchRect = new Rect(inRect.x, y, inRect.width, SearchHeight);
        searchText = Widgets.TextField(searchRect, searchText ?? "");
        y += SearchHeight + 10f;

        // Reserve space for the close button
        const float bottomButtonHeight = 35f;
        const float bottomButtonWidth = 140f;
        const float bottomPadding = 8f;

        Rect listRect = new Rect(inRect.x, y, inRect.width, inRect.height - (y - inRect.y) - (bottomButtonHeight + bottomPadding));
        DrawScrollList(listRect);

        Rect closeRect = new Rect(
            inRect.xMax - bottomButtonWidth,
            inRect.yMax - bottomButtonHeight,
            bottomButtonWidth,
            bottomButtonHeight
        );

        if (Widgets.ButtonText(closeRect, "Close"))
        {
            Close(doCloseSound: true);
        }
    }

    internal void DrawScrollList(Rect outRect)
    {
        RebuildEntryCacheIfNeeded();

        float viewHeight = _cachedViewHeight;
        if (viewHeight < outRect.height) viewHeight = outRect.height + 1f;

        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

        float visibleMin = scrollPos.y;
        float visibleMax = scrollPos.y + outRect.height;

        int startIndex = FindFirstVisibleIndex(visibleMin);
        if (startIndex < 0) startIndex = 0;

        for (int i = startIndex; i < _cachedEntries.Count; i++)
        {
            float y = _prefixY[i];
            string primaryPath = _cachedEntries[i].Value.Item2;
            float h = EstimateEntryHeight(primaryPath);

            if (y > visibleMax)
                break;

            Rect entryRect = new Rect(0f, y, viewRect.width, h);
            var kvp = _cachedEntries[i];

            DrawEntry(entryRect, kvp.Key, primaryPath);
        }

        Widgets.EndScrollView();
    }

    private void RebuildEntryCacheIfNeeded()
    {
        string s = searchText ?? "";

        if (_cachedEntries != null && _cachedSearch == s)
            return;

        _cachedSearch = s;
        _cachedEntries = GetFilteredEntries(s);

        int n = _cachedEntries.Count;
        _prefixY = new float[n + 1];
        float y = 0f;

        for (int i = 0; i < n; i++)
        {
            string primaryPath = _cachedEntries[i].Value.Item2;
            float h = EstimateEntryHeight(primaryPath);

            _prefixY[i] = y;
            y += h + RowGap;
        }

        _prefixY[n] = y;
        _cachedViewHeight = y;
    }

    private int FindFirstVisibleIndex(float scrollY)
    {
        // We want the first i such that entryBottom(i) >= scrollY.
        // entryBottom(i) = _prefixY[i] + height(i)
        // We'll do a simple binary search over i using bottom values.
        int lo = 0;
        int hi = _cachedEntries.Count - 1;
        int result = _cachedEntries.Count;

        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            float startY = _prefixY[mid];
            float h = EstimateEntryHeight(_cachedEntries[mid].Value.Item2);
            float bottomY = startY + h;

            if (bottomY >= scrollY)
            {
                result = mid;
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return result;
    }

    internal abstract List<KeyValuePair<Def, (HashSet<string>, string)>> GetFilteredEntries(string search);

    // TODO I think I planned to make this return a dynamic height based on tile count, for if they have to be shrunk to fit horizontally
    private float EstimateEntryHeight(string path)
    {
        int modCount = 0;
        if (!path.NullOrEmpty() && Caching.pathToModsDict.TryGetValue(path, out var mods) && mods != null)
        {
            modCount = mods.Count;
        }

        float tileSize = TileMaxHeight;
        return DefLabelHeight + 6f + tileSize + 6f + ModLabelHeight;
    }

    internal virtual void DrawEntry(Rect rect, Def def, string path)
    {
        if (def == null || path.NullOrEmpty())
            return;

        Widgets.DrawMenuSection(rect);

        Rect inner = rect.ContractedBy(10f);

        string defLabel = def.LabelCap;
        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(new Rect(inner.x, inner.y, inner.width, DefLabelHeight), defLabel);
        Text.Anchor = TextAnchor.UpperLeft;

        float y = inner.y + DefLabelHeight + 6f;

        if (!Caching.pathToModsDict.TryGetValue(path, out var mods) || mods == null || mods.Count == 0)
        {
            Widgets.Label(new Rect(inner.x, y, inner.width, 24f), $"No sources found for: {path}");
            return;
        }

        TP_Settings.chosenPathToModDict.TryGetValue(path, out ModContentPack chosenMod);

        int count = mods.Count;
        float availableWidth = inner.width;
        float tileWidth = Mathf.Max(TileMinWidth, (availableWidth - (count - 1) * TilePadding) / count);

        float tileTexSize = Mathf.Min(TileMaxHeight, tileWidth);
        float tileHeight = tileTexSize + 6f + ModLabelHeight;

        Rect tilesRect = new Rect(inner.x, y, inner.width, tileHeight);

        for (int i = 0; i < count; i++)
        {
            ModContentPack mod = mods[i];
            Rect tileRect = new Rect(
                tilesRect.x + i * (tileWidth + TilePadding),
                tilesRect.y,
                tileWidth,
                tileHeight
            );

            DrawTextureTile(tileRect, path, def, mod, chosenMod);
        }
    }

    internal virtual void DrawTextureTile(Rect tileRect, string path, Def def, ModContentPack mod, ModContentPack chosenMod)
    {
        Widgets.DrawMenuSection(tileRect);

        bool isChosen = (chosenMod != null && mod == chosenMod);

        if (isChosen)
        {
            Widgets.DrawHighlightSelected(tileRect);
        }

        Rect inner = tileRect.ContractedBy(6f);

        float texSize = Mathf.Min(inner.width, TileMaxHeight);
        Rect texRect = new Rect(inner.x + (inner.width - texSize) * 0.5f, inner.y, texSize, texSize);

        Texture2D tex = null;
        try
        {
            if (!mod.IsOfficialMod)
            {
                tex = mod.GetContentHolder<Texture2D>().Get(path);
            }
            else
            {
                tex = (Texture2D)Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + path);
            }
        }
        catch (Exception e)
        {
            Log.ErrorOnce($"Failed to retrieve texture from {path}", path.GetHashCode());
        }

        if (tex != null)
        {
            GUI.DrawTexture(texRect, tex, ScaleMode.ScaleToFit);
        }
        else
        {
            Widgets.DrawBoxSolid(texRect, new Color(0f, 0f, 0f, 0.15f));
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(texRect, "Missing");
            Text.Anchor = TextAnchor.UpperLeft;
        }

        if (Widgets.ButtonInvisible(tileRect))
        {
            SetAllGraphicPathsForDef(path, def, mod);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }

        Rect modLabelRect = new Rect(inner.x, texRect.yMax + 6f, inner.width, ModLabelHeight);
        string modLabel = GetModDisplayName(mod);

        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(modLabelRect, modLabel);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    internal abstract (HashSet<string>, string) DefaultGraphicForDef(Def def);

    internal abstract (HashSet<string>, string) AllGraphicPathsForDefInMod(Def def, ModContentPack mod);

    internal abstract void PopulateOverloadedItemsDict();

    internal abstract void SetAllGraphicPathsForDef(string path, Def def, ModContentPack mod);

    internal static IEnumerable<string> DirectionPaths(string basePath)
    {
        for (int i = 0; i < directionSuffix.Length; i++)
        {
            yield return basePath + directionSuffix[i];
        }
    }

    // Better compatibility with modded bodyTypes for all those Asian gooner mods
    internal static IEnumerable<string> BodyTypeDirectionPaths(string basePath)
    {
        var bodyTypes = DefDatabase<BodyTypeDef>.AllDefsListForReading;
        for (int i = 0; i < bodyTypes.Count; i++)
        {
            string bodyType = bodyTypes[i].defName;
            for (int j = 0; j < directionSuffix.Length; j++)
            {
                yield return $"{basePath}_{bodyType}{directionSuffix[j]}";
            }
        }
    }

    internal static IEnumerable<string> StackCountPaths(string basePath)
    {
        for (char ch = 'a'; ch <= 'z'; ch++)
        {
            string path = $"{basePath}_{ch}";
            if (!TextureExists(path))
                break;

            yield return path;
        }

        for (int i = 0; i < 10000; i++)
        {
            string path = $"{basePath}_{i}";
            if (!TextureExists(path))
                break;

            yield return path;
        }
    }

    internal static bool TextureExists(string texPath)
    {
        return Caching.pathToModsDict.ContainsKey(texPath);
    }

    internal static bool TextureExistsInMod(string texPath, ModContentPack mod)
    {
        if (mod == null || texPath.NullOrEmpty())
            return false;

        return mod.textures.contentList.ContainsKey(texPath);
    }

    internal static string GetModDisplayName(ModContentPack mod)
    {
        if (mod == null)
            return "(null mod)";

        string modName = mod.Name;
        string pkg = mod.PackageIdPlayerFacing;

        return modName ?? pkg ?? "(unknown)";
    }
}
