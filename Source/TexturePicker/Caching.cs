using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using System.Reflection;
using HarmonyLib;

namespace nuff.ChooseYourTextures;

[StaticConstructorOnStartup]
public static class Caching
{
    internal static Dictionary<string, List<ModContentPack>> pathToModsDict = new Dictionary<string, List<ModContentPack>>();
    internal static HashSet<string> overloadedPaths = new HashSet<string>();
    internal static List<FieldInfo> pawnKindLifeStageFields = new List<FieldInfo>();

    static Caching()
    {
        PopulatePathToModsDict();
        PopulateOverLoadedPaths();
        PopulatePawnKindLifeStageFields();
    }

    static void PopulatePathToModsDict()
    {
        // Populate content from mods
        List<ModContentPack> runningModsListForReading = LoadedModManager.RunningModsListForReading;
        for (int i = runningModsListForReading.Count - 1; i >= 0; i--)
        {
            ModContentHolder<Texture2D> textures = runningModsListForReading[i].textures;
            if (textures == null || textures.contentList.NullOrEmpty())
                continue;

            foreach (var kvp in textures.contentList)
            {
                string key = kvp.Key;
                pathToModsDict.TryGetValue(key, out var list);
                if (list == null)
                {
                    list = new List<ModContentPack>();
                    pathToModsDict[key] = list;
                }
                list.Add(runningModsListForReading[i]);
            }
        }

        //ModContentPack core = runningModsListForReading.First(mcp => mcp.Name == "Core");

        //// Try to also add vanilla textures matching the mod textures
        //foreach (var kvp in pathToModsDict)
        //{
        //    string key = kvp.Key;
        //    Texture2D tex = (Texture2D)Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + key);
        //    if (tex != null)
        //    {
        //        pathToModsDict.TryGetValue(key, out var list);
        //        if (!list.NullOrEmpty())
        //        {
        //            list.Add(core);
        //        }
        //    }
        //}
    }

    static void PopulateOverLoadedPaths()
    {
        overloadedPaths = new HashSet<string>();
        foreach (var kvp in Caching.pathToModsDict)
        {
            string key = kvp.Key;
            Caching.pathToModsDict.TryGetValue(key, out var list);
            if (list.Count >= 2)
            {
                overloadedPaths.Add(key);
            }
        }
    }

    static void PopulatePawnKindLifeStageFields()
    {
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:bodyGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:dessicatedBodyGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleDessicatedBodyGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:corpseGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:swimmingGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleSwimmingGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleCorpseGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:silhouetteGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:rottingGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleRottingGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:stationaryGraphicData"));
        pawnKindLifeStageFields.Add(AccessTools.Field("PawnKindLifeStage:femaleStationaryGraphicData"));
    }

    internal static bool IsOverloaded(string path)
    {
        return overloadedPaths.Contains(path);
    }
}
