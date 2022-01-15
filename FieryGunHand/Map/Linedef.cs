using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryGunHand.Map
{
    public class Linedef
    {
        [Flags]
        public enum LinedefFlags
        {
            Impassible = 0x0001,
            BlockMonsters = 0x0002,
            DoubleSided = 0x0004,
            LowerUnpegged = 0x0008,
            UpperUnpegged = 0x0010,
            Secret = 0x0020,
            Blocksound = 0x0040,
            HideOnAutomap = 0x0080,
            AlwaysShowOnAutomap = 0x0100
        }

        public int StartVertex;
        public int EndVertex;
        public LinedefFlags Flags;
        public int Special;
        public int SectorTag;
        public int FrontSidedef;
        public int BackSidedef;

        public float Length;
    }
}
