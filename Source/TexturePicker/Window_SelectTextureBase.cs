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

public class Window_SelectTextureBase : Window
{
    private static readonly string[] directionSuffix =
    {
        "_north",
        "_south",
        "_east",
        "_west"
    };

    private string searchText = "";
    private Vector2 scrollPos;

    private const float TitleHeight = 40f;
    private const float SearchHeight = 32f;
    private const float RowGap = 14f;
    private const float DefLabelHeight = 26f;
    private const float ModLabelHeight = 22f;
    private const float TilePadding = 8f;
    private const float TileMinWidth = 160f;
    private const float TileMaxHeight = 160f;

    public override Vector2 InitialSize => new Vector2(1000, 750);

    public Window_SelectTextureBase()
    {
        doCloseX = true;
        closeOnAccept = false;
        closeOnCancel = true;
        absorbInputAroundWindow = true;
    }
}
