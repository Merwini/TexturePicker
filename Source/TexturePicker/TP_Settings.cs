using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.ChooseYourTextures;

public class TP_Settings : ModSettings
{
    internal static Dictionary<string, ModContentPack> chosenPathToModDict = new Dictionary<string, ModContentPack>();

    private static List<string> pathsForSaving = new List<string>();
    private static List<string> packageIdsForSaving = new List<string>();

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            StringifyDict();
        }

        Scribe_Collections.Look(ref pathsForSaving, "pathsForSaving");
        Scribe_Collections.Look(ref packageIdsForSaving, "packageIdsForSaving");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            DestringifyDict();
        }
    }

    void StringifyDict()
    {
        pathsForSaving = new List<string>();
        packageIdsForSaving = new List<string>();
        foreach (var kvp in chosenPathToModDict)
        {
            pathsForSaving.Add(kvp.Key);
            packageIdsForSaving.Add(kvp.Value.PackageId);
        }
    }

    void DestringifyDict()
    {
        chosenPathToModDict.Clear();
        if (!pathsForSaving.NullOrEmpty())
        {
            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
            for (int i = 0; i < pathsForSaving.Count; i++)
            {
                string pid = packageIdsForSaving[i];
                ModContentPack mcp = mods.FirstOrDefault(x => x.PackageId == pid);
                if (mcp == null)
                {
                    continue;
                }

                chosenPathToModDict[pathsForSaving[i]] = mcp;
            }
        }
    }

    internal static void InitRelevantThingDefs()
    {
        GraphicDatabase.Clear(); // TODO would it be more performant to try to find and clear only relevant items?

        List<ThingDef> thingDefs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < thingDefs.Count; i++)
        {
            GraphicData graphic = thingDefs[i].graphicData;
            if (graphic == null)
                continue;

            string texPath = graphic.texPath;
            if (texPath == null)
                continue;

            if (pathsForSaving.Contains(texPath))
                graphic.Init();
        }
    }
}
