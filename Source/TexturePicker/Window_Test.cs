using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace ChooseYourTextures
{
    internal class Window_Test : Window
    {
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(inRect, "test");
        }
    }
}
