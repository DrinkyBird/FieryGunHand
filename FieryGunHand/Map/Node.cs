using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryGunHand.Map
{
    public class Node
    {
        public Line Line;
        public int RightBoundingBoxTop;
        public int RightBoundingBoxBottom;
        public int RightBoundingBoxLeft;
        public int RightBoundingBoxRight;
        public int LeftBoundingBoxTop;
        public int LeftBoundingBoxBottom;
        public int LeftBoundingBoxLeft;
        public int LeftBoundingBoxRight;
        public int RightChild;
        public int LeftChild;
    }
}
