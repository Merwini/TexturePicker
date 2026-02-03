using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace TexturePicker;

public class TP_Settings : ModSettings
{
    internal static Dictionary<string, ModContentPack> chosenPathToModDict = new Dictionary<string, ModContentPack>();
    private List<string> workingKeys = new List<string>();
    private List<ModContentPack> workingValues = new List<ModContentPack>();

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Collections.Look(ref chosenPathToModDict, "chosenPathToModDict", LookMode.Value, LookMode.Reference, ref workingKeys, ref workingValues);
    }
}
