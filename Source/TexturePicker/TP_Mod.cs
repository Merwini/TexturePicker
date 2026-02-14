using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace nuff.ChooseYourTextures;

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

        if (Current.Game != null)
        {
            listing.Label("SETTINGS MUST BE CHANGED FROM THE MAIN MENU.");
        }
        else
        {
            listing.Label("Manage texture overrides:");

            if (listing.ButtonText("Open Thing Texture Override Manager"))
            {
                Find.WindowStack.Add(new Window_SelectThingTextures());
            }

            listing.Gap();

            if (listing.ButtonText("Open Pawn / Animal Texture Override Manager"))
            {
                Find.WindowStack.Add(new Window_SelectPawnTextures());
            }
        }

        listing.End();
    }
}
