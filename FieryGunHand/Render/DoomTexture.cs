using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryGunHand.Render
{
    public class DoomTexture
    {
        public class Patch
        {
            public int OriginX;
            public int OriginY;
            public int PatchIndex; // in PNAMES
            public int StepDir;
            public int ColourMap;
        }

        public string Name;
        public int Masked;
        public int Width;
        public int Height;
        public int ColumnDirectory;
        public Patch[] Patches;
    }
}
