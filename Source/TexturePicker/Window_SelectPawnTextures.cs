using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using RimWorld.BaseGen;
using System.Reflection;
using HarmonyLib;

namespace ChooseYourTextures;

public class Window_SelectPawnTextures : Window_SelectTextureBase
{
    Dictionary<PawnKindDef, (HashSet<string>, string)> pawnKindDefToPathsDict = new Dictionary<PawnKindDef, (HashSet<string>, string)>();
    Dictionary<PawnKindDef, (HashSet<string>, string)> overloadedPawnKindDefs = new Dictionary<PawnKindDef, (HashSet<string>, string)>();

    List<FieldInfo> fieldInfos;
    List<GraphicData> allGraphicDatas;

    public Window_SelectPawnTextures() : base()
    {
    }

    public override void PreOpen()
    {
        base.PreOpen();

        pawnKindDefToPathsDict = new Dictionary<PawnKindDef, (HashSet<string>, string)>();
        overloadedPawnKindDefs = new Dictionary<PawnKindDef, (HashSet<string>, string)>();
        allGraphicDatas = new List<GraphicData>();
        fieldInfos = Caching.pawnKindLifeStageFields;

        List<PawnKindDef> defs = DefDatabase<PawnKindDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            PawnKindDef def = defs[i];
            pawnKindDefToPathsDict[def] = DefaultGraphicForDef(def);
        }
    }

    public override void PreClose()
    {
        base.PreClose();

        for (int i = 0; i < allGraphicDatas.Count; i++)
        {
            allGraphicDatas[i].Init();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        base.DoWindowContents(inRect);
    }

    internal override (HashSet<string>, string) DefaultGraphicForDef(Def def)
    {
        PawnKindDef pawnKindDef = def as PawnKindDef;
        List<PawnKindLifeStage> lifeStages = pawnKindDef.lifeStages;
        if (lifeStages.NullOrEmpty())
            return (new HashSet<string>(), null);

        for (int i = lifeStages.Count - 1; i > 0; i--)
        {
            PawnKindLifeStage stage = lifeStages[i];
            for (int j = 0; j < fieldInfos.Count; j++)
            {
                GraphicData graphicData = fieldInfos[j].GetValue(stage) as GraphicData;
                if (graphicData == null)
                    continue;

                string texPath = graphicData.texPath;
                if (texPath == null)
                    continue;

                string texEast = texPath + "_east";
                if (TextureExists(texEast))
                {
                    return (new HashSet<string> {texEast}, texEast);
                }
            }
        }

        return (new HashSet<string>(), null);
    }

    internal override (HashSet<string>, string) AllGraphicPathsForDefInMod(Def def, ModContentPack mod)
    {
        PawnKindDef pawnKindDef = def as PawnKindDef;
        HashSet<string> allPaths = new HashSet<string>();
        string primaryGraphic = null;
        List<FieldInfo> fieldInfos = Caching.pawnKindLifeStageFields;

        List<PawnKindLifeStage> lifeStages = pawnKindDef.lifeStages;
        if (lifeStages.NullOrEmpty())
            return (new HashSet<string>(), null);

        for (int i = 0; i < lifeStages.Count; i++)
        {
            PawnKindLifeStage stage = lifeStages[i];
            for (int j = 0; j < fieldInfos.Count; j++)
            {
                bool pickedPrimaryForStage = false;
                GraphicData graphicData = fieldInfos[j].GetValue(stage) as GraphicData;
                if (graphicData == null)
                    continue;

                string texPath = graphicData.texPath;
                if (texPath == null)
                    continue;

                allGraphicDatas.Add(graphicData); // So I can more easily .Init them all in PreClose()

                foreach (string directionPath in DirectionPaths(texPath))
                {
                    if (TextureExists(directionPath))
                    {
                        allPaths.Add(directionPath);

                        // Set first _east graphic in that stage as the primary graphic. This makes it so the first _east in the last PawnKindLifeStage will be the display graphic
                        if (!pickedPrimaryForStage && directionPath.EndsWith("_east"))
                        {
                            primaryGraphic = directionPath;
                            pickedPrimaryForStage = true;
                        }
                    }
                }
            }
        }

        return (allPaths, primaryGraphic);
    }

    internal override void PopulateOverloadedItemsDict()
    {
        foreach (var kvp in pawnKindDefToPathsDict)
        {
            HashSet<string> hashSet = kvp.Value.Item1;
            foreach (var str in hashSet)
            {
                if (Caching.overloadedPaths.Contains(str))
                {
                    overloadedPawnKindDefs[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    internal override void SetAllGraphicPathsForDef(string path, Def def, ModContentPack mod)
    {
        PawnKindDef pawnKindDef = def as PawnKindDef;
        (HashSet<string>, string) strings = AllGraphicPathsForDefInMod(def, mod);

        //overloadedPawnKindDefs.TryGetValue(pawnKindDef, out (HashSet<string>, string) value);
        //foreach (var str in value.Item1)
        foreach (var str in strings.Item1)
        {
            if (TextureExistsInMod(str, mod))
            {
                TP_Settings.chosenPathToModDict[str] = mod;
            }
        }
    }

    internal override List<KeyValuePair<Def, (HashSet<string>, string)>> GetFilteredEntries(string search)
    {
        IEnumerable<KeyValuePair<Def, (HashSet<string>, string)>> entries = pawnKindDefToPathsDict.Where(kvp => Caching.IsOverloaded(kvp.Value.Item2)).Select(kvp => new KeyValuePair<Def, (HashSet<string>, string)>(kvp.Key, kvp.Value));

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
}
