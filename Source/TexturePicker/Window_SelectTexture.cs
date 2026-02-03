using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseYourTextures;

public class Window_SelectTexture : Window
{

    Dictionary<ThingDef, string> thingDefToPathDict = new Dictionary<ThingDef, string>();

    private string searchText = "";
    private Vector2 scrollPos;

    private const float TitleHeight = 40f;
    private const float SearchHeight = 32f;
    private const float RowGap = 14f;
    private const float DefLabelHeight = 26f;
    private const float ModLabelHeight = 22f;
    private const float TilePadding = 8f;
    private const float TileMinWidth = 160f;
    private const float TileMaxHeight = 160f;

    public override Vector2 InitialSize => new Vector2(1000, 750);

    public Window_SelectTexture()
    {
        doCloseX = true;
        closeOnAccept = false;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        thingDefToPathDict = new Dictionary<ThingDef, string>();
        HashSet<string> paths = new HashSet<string>();

        foreach (var kvp in Caching.pathToModsDict)
        {
            string key = kvp.Key;
            Caching.pathToModsDict.TryGetValue(key, out var list);
            if (list.Count >= 2)
            {
                paths.Add(key);
            }
        }

        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            GraphicData graphicData = defs[i].graphicData;
            if (graphicData != null && graphicData.texPath is string path && paths.Contains(path))
            {
                thingDefToPathDict[defs[i]] = path;
            }
        }
    }

    public override void PreClose()
    {
        base.PreClose();

        GraphicDatabase.Clear();
        foreach (var kvp in thingDefToPathDict)
        {
            var key = kvp.Key;
            key.graphicData.Init();
        }
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

        // Reserve space for the close button at the bottom
        const float bottomButtonHeight = 35f;
        const float bottomButtonWidth = 140f;
        const float bottomPadding = 8f;

        Rect listRect = new Rect(inRect.x, y, inRect.width, inRect.height - (y - inRect.y) - (bottomButtonHeight + bottomPadding));
        DrawScrollList(listRect);

        // Close button (bottom-right)
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

    private void DrawScrollList(Rect outRect)
    {
        List<KeyValuePair<ThingDef, string>> entries = GetFilteredEntries(searchText);

        float viewHeight = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            viewHeight += EstimateEntryHeight(entries[i].Value);
            viewHeight += RowGap;
        }
        if (viewHeight < outRect.height) viewHeight = outRect.height + 1f;

        Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, viewHeight);

        Widgets.BeginScrollView(outRect, ref scrollPos, viewRect);

        float curY = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var kvp = entries[i];
            float entryHeight = EstimateEntryHeight(kvp.Value);

            Rect entryRect = new Rect(0f, curY, viewRect.width, entryHeight);
            DrawThingEntry(entryRect, kvp.Key, kvp.Value);

            curY += entryHeight + RowGap;
        }

        Widgets.EndScrollView();
    }

    private List<KeyValuePair<ThingDef, string>> GetFilteredEntries(string search)
    {
        IEnumerable<KeyValuePair<ThingDef, string>> dict = thingDefToPathDict;

        if (!search.NullOrEmpty())
        {
            string lowercase = search.ToLowerInvariant();
            dict = dict.Where(kvp => (kvp.Key?.label ?? kvp.Key?.defName ?? "").ToLowerInvariant().Contains(lowercase));
        }

        return dict
            .OrderBy(kvp => kvp.Key?.label ?? kvp.Key?.defName ?? "")
            .ThenBy(kvp => kvp.Key?.defName ?? "")
            .ToList();
    }

    private float EstimateEntryHeight(string path)
    {
        int modCount = 0;
        if (!path.NullOrEmpty() && Caching.pathToModsDict.TryGetValue(path, out var mods) && mods != null)
            modCount = mods.Count;

        float tileSize = TileMaxHeight;
        return DefLabelHeight + 6f + tileSize + 6f + ModLabelHeight;
    }

    private void DrawThingEntry(Rect rect, ThingDef def, string path)
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

            DrawTextureTile(tileRect, path, mod, chosenMod);
        }
    }

    private void DrawTextureTile(Rect tileRect, string path, ModContentPack mod, ModContentPack chosenMod)
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

        if (Widgets.ButtonInvisible(texRect))
        {
            TP_Settings.chosenPathToModDict[path] = mod;
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }

        Rect modLabelRect = new Rect(inner.x, texRect.yMax + 6f, inner.width, ModLabelHeight);
        string modLabel = GetModDisplayName(mod);

        Text.Anchor = TextAnchor.UpperCenter;
        Widgets.Label(modLabelRect, modLabel);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static string GetModDisplayName(ModContentPack mod)
    {
        if (mod == null)
            return "(null mod)";

        string modName = mod.Name;
        string pkg = mod.PackageIdPlayerFacing;

        return modName ?? pkg ?? "(unknown)";
    }
}
