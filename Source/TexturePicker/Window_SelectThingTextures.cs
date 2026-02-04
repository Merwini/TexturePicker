using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ChooseYourTextures;

public class Window_SelectThingTextures : Window_SelectTextureBase
{
    Dictionary<ThingDef, string> thingDefToPathDict = new Dictionary<ThingDef, string>();

    public Window_SelectThingTextures() : base()
    {
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
}
