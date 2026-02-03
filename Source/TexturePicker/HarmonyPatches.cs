using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using HarmonyLib;

namespace ChooseYourTextures;

[StaticConstructorOnStartup]
public class HarmonyPatches
{
    static HarmonyPatches()
    {
        Harmony harmony = new Harmony("nuff.ChooseYourTextures");

        harmony.PatchAll();

        TP_Settings.InitRelevantThingDefs();
    }

    [HarmonyPatch(typeof(ContentFinder<Texture2D>), nameof(ContentFinder<Texture2D>.Get))]
    public static class ContentFinder_Get_Prefix
    {
        public static bool Prefix(string itemPath, ref Texture2D __result)
        {
            if (TP_Settings.chosenPathToModDict.TryGetValue(itemPath, out var mod))
            {
                if (!mod.IsOfficialMod)
                {
                    __result = mod.GetContentHolder<Texture2D>().Get(itemPath);
                    return false;
                }
                else if (mod.IsOfficialMod)
                {
                    __result = (Texture2D)(object)Resources.Load<Texture2D>(GenFilePaths.ContentPath<Texture2D>() + itemPath);
                    return false;
                }
                else
                {
                    Log.Error("This should never happen");
                }
            }

            return true;
        }
    }
}
