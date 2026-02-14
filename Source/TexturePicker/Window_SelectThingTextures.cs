using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseYourTextures;

public class Window_SelectThingTextures : Window_SelectTextureBase
{
    Dictionary<ThingDef, (HashSet<string>, string)> thingDefToPathsDict = new Dictionary<ThingDef, (HashSet<string>, string)>();
    Dictionary<ThingDef, (HashSet<string>, string)> overloadedThingDefs = new Dictionary<ThingDef, (HashSet<string>, string)>();

    public Window_SelectThingTextures()
    {
        doCloseX = true;
        closeOnAccept = false;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }

    public override void PreOpen()
    {
        base.PreOpen();

        thingDefToPathsDict = new Dictionary<ThingDef, (HashSet<string>, string)>();
        overloadedThingDefs = new Dictionary<ThingDef, (HashSet<string>, string)>();

        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            thingDefToPathsDict[def] = DefaultGraphicForDef(def);
        }

        PopulateOverloadedItemsDict();
    }

    public override void PreClose()
    {
        base.PreClose();

        foreach (var kvp in overloadedThingDefs)
        {
            var key = kvp.Key;
            key.graphicData.Init();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
    }

    internal override (HashSet<string>, string) DefaultGraphicForDef(Def def)
    {
        ThingDef thingDef = def as ThingDef;
        GraphicData graphicData = thingDef.graphicData;
        if (graphicData == null)
            return (new HashSet<string>(), null);

        string basePath = null;
        if (graphicData.graphicClass == typeof(Graphic_Single))
        {
            basePath = graphicData.texPath;
        }
        if (graphicData.graphicClass == typeof(Graphic_StackCount))
        {
            basePath = StackCountBasePath(graphicData.texPath) + "_a";
        }
        if (graphicData.graphicClass == typeof(Graphic_Multi))
        {
            basePath = graphicData.texPath + "_east";
        }
        if (basePath == null)
            return (new HashSet<string>(), null);

        return (new HashSet<string> { basePath }, basePath);
    }

    internal override (HashSet<string>, string) AllGraphicPathsForDefInMod(Def def, ModContentPack mod)
    {
        ThingDef thingDef = def as ThingDef;
        HashSet<string> allPaths = new HashSet<string>();
        string primaryPath = null;

        GraphicData graphicData = thingDef.graphicData;
        if (graphicData == null)
            return (new HashSet<string>(), null);

        string basePath = null;

        if (graphicData.graphicClass == typeof(Graphic_Single))
        {
            basePath = graphicData.texPath;
            if (basePath.NullOrEmpty())
                return (new HashSet<string>(), null);

            primaryPath = basePath;
            allPaths.Add(basePath);

            return (allPaths, primaryPath);
        }

        if (graphicData.graphicClass == typeof(Graphic_Multi))
        {
            basePath = graphicData.texPath;
            if (basePath.NullOrEmpty())
                return (new HashSet<string>(), null);

            string east = basePath + "_east";
            primaryPath = TextureExistsInMod(east, mod) ? east : basePath;

            foreach (string path in DirectionPaths(basePath))
            {
                if (TextureExistsInMod(path, mod))
                {
                    allPaths.Add(path);
                }
            }

            foreach (string path in BodyTypeDirectionPaths(basePath))
            {
                if (TextureExistsInMod(path, mod))
                {
                    allPaths.Add(path);
                }
            }

            allPaths.Add(primaryPath);
            return (allPaths, primaryPath);
        }

        if (graphicData.graphicClass == typeof(Graphic_StackCount))
        {
            string stackBase = StackCountBasePath(graphicData.texPath);
            if (stackBase.NullOrEmpty())
                return (new HashSet<string>(), null);

            IEnumerable<string> stackCountPaths = StackCountPaths(stackBase);
            primaryPath = stackCountPaths.First();

            foreach (string path in stackCountPaths)
            {
                allPaths.Add(path);
            }

            allPaths.Add(primaryPath);
            return (allPaths, primaryPath);
        }

        if (basePath == null)
            return (new HashSet<string>(), null);

        allPaths.Add(basePath);

        foreach (string directionPath in DirectionPaths(basePath))
        {
            if (TextureExistsInMod(directionPath, mod))
            {
                allPaths.Add(directionPath);
            }
        }

        foreach (string bodyDirectionPath in BodyTypeDirectionPaths(basePath))
        {
            if (TextureExistsInMod(bodyDirectionPath, mod))
            {
                allPaths.Add(bodyDirectionPath);
            }
        }

        foreach (string stackCountPath in StackCountPaths(basePath))
        {
            if (TextureExistsInMod(stackCountPath, mod))
            {
                allPaths.Add(stackCountPath);
            }
        }

        return (allPaths, primaryPath);
    }

    internal override void PopulateOverloadedItemsDict()
    {
        foreach (var kvp in thingDefToPathsDict)
        {
            HashSet<string> hashSet = kvp.Value.Item1;
            foreach (var str in hashSet)
            {
                if (Caching.IsOverloaded(str))
                {
                    overloadedThingDefs[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    internal override void SetAllGraphicPathsForDef(string path, Def def, ModContentPack mod)
    {
        ThingDef thingDef = def as ThingDef;
        (HashSet<string>, string) strings = AllGraphicPathsForDefInMod(def, mod);

        //overloadedThingDefs.TryGetValue(thingDef, out (HashSet<string>, string) value);
        //foreach (var str in value.Item1)
        foreach (var str in strings.Item1)
        {
            TP_Settings.chosenPathToModDict[str] = mod;
        }
    }


    internal override List<KeyValuePair<Def, (HashSet<string>, string)>> GetFilteredEntries(string search)
    {
        IEnumerable<KeyValuePair<Def, (HashSet<string>, string)>> entries = thingDefToPathsDict.Where(kvp => Caching.IsOverloaded(kvp.Value.Item2)).Select(kvp => new KeyValuePair<Def, (HashSet<string>, string)>(kvp.Key, kvp.Value));

        if (!search.NullOrEmpty())
        {
            string lowercase = search.ToLowerInvariant();
            entries = entries.Where(kvp => (kvp.Key?.label ?? kvp.Key?.defName ?? "").ToLowerInvariant().Contains(lowercase));
        }

        return entries
            .OrderBy(kvp => kvp.Key?.label ?? kvp.Key?.defName ?? "")
            .ThenBy(kvp => kvp.Key?.defName ?? "")
            .ToList();
    }

    private static string StackCountBasePath(string path)
    {
        if (path.NullOrEmpty())
            return null;

        int lastSlash = path.LastIndexOf('/');
        string lastPart = (lastSlash >= 0 && lastSlash < path.Length - 1) ? path.Substring(lastSlash + 1) : path;

        return $"{path}/{lastPart}";
    }
}
