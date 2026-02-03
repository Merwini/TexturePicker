using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace ChooseYourTextures;

[StaticConstructorOnStartup]
public static class Caching
{
    internal static Dictionary<string, List<ModContentPack>> pathToModsDict = new Dictionary<string, List<ModContentPack>>();

    static Caching()
    {
        PopulatePathToModsDict();
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

        ModContentPack core = runningModsListForReading.First(mcp => mcp.Name == "Core");

        // Try to also add vanilla textures matching the mod textures
        foreach (var kvp in pathToModsDict)
        {
            string key = kvp.Key;
            Texture2D tex = (Texture2D)(object)Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + key);
            if (tex != null)
            {
                pathToModsDict.TryGetValue(key, out var list);
                if (!list.NullOrEmpty())
                {
                    list.Add(core);
                }
            }
        }
    }
}
