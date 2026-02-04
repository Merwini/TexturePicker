using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;

namespace ChooseYourTextures;

public class Window_SelectPawnTextures : Window_SelectTextureBase
{
    Dictionary<PawnKindDef, string> pawnKindDefToPathDict = new Dictionary<PawnKindDef, string>();

    public Window_SelectPawnTextures() : base()
    {
    }

    public override void PreOpen()
    {
        base.PreOpen();

        pawnKindDefToPathDict = new Dictionary<PawnKindDef, string>();
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

        List<PawnKindDef> defs = DefDatabase<PawnKindDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            List<>
            GraphicData graphicData = defs[i].graphicData;
            if (graphicData != null && graphicData.texPath is string path && paths.Contains(path))
            {
                pawnKindDefToPathDict[defs[i]] = path;
            }
        }
    }
}
