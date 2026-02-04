using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace ChooseYourTextures;

public class TP_Mod : Mod
{
    TP_Settings Settings;

    public TP_Mod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<TP_Settings>();
    }

    public override string SettingsCategory() => "Choose Your Textures";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        Listing_Standard listing = new Listing_Standard();
        listing.Begin(inRect);

        listing.Label("Manage texture overrides:");

        if (listing.ButtonText("Open Texture Override Manager"))
        {
            Find.WindowStack.Add(new Window_SelectTexture());
        }
        if (listing.ButtonText("Open Pawn Texture Override Manager"))
        {
            Find.WindowStack.Add(new Window_SelectTexture());
        }

        listing.End();
    }
}
